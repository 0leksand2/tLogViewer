using tLogViewer.Core.Enums.Heartbeat;
using tLogViewer.Core.Models;
using tLogViewer.Core.Models.Messages;
using tLogViewer.DTO.Messages;

namespace tLogViewer.Services.Services;

/// <summary>
/// Builds synthetic DERIVED (998) samples once per second (first millisecond):
/// link quality (MP seq-gap / 3 s window) and seconds since arm.
/// </summary>
public sealed class DerivedMessageCalculator
{
    private const int WindowSeconds = 3;
    private const byte GcsSysId = 255;
    private const byte CompanionGcsSysId = 253;
    private const string DerivedMessageId = "998";

    private readonly Dictionary<(byte SysId, byte CompId), int> _recvPacketCount = new();
    private readonly Queue<(int Received, int Lost)> _window = new();
    private readonly List<MavMessageDto> _samples = new();

    private ulong? _currentSecond;
    private int _secondReceived;
    private int _secondLost;

    private bool? _wasArmed;
    private ulong? _armedFromUs;
    private bool _armed;
    private (byte SysId, byte CompId)? _armSource;

    public void ObservePacket(TLogRecord record)
    {
        var packet = record.MavPacket;
        if (packet.SysId == GcsSysId || packet.SysId == CompanionGcsSysId)
        {
            return;
        }

        AdvanceTo(record.Trail / 1_000_000UL);

        var key = (packet.SysId, packet.CompId);
        var packetSeqNo = packet.Seq;

        if (!_recvPacketCount.TryGetValue(key, out var recvPacketCount))
        {
            // First packet for this sys/comp — seed like MP (no loss credited).
            _recvPacketCount[key] = unchecked((byte)(packetSeqNo - 1));
            recvPacketCount = _recvPacketCount[key];
        }

        var expectedPacketSeqNo = (recvPacketCount + 1) % 0x100;

        // Duplicate seq (SiK bug) — still count received, do not count loss.
        if (packetSeqNo != expectedPacketSeqNo && packetSeqNo != recvPacketCount)
        {
            int numLost;
            if (packetSeqNo < (recvPacketCount + 1))
            {
                numLost = 0x100 - expectedPacketSeqNo + packetSeqNo;
            }
            else
            {
                numLost = packetSeqNo - expectedPacketSeqNo;
            }

            _secondLost += numLost;
        }

        _recvPacketCount[key] = packetSeqNo;
        _secondReceived++;
    }

    public void ObserveHeartbeat(TLogRecord record, Heartbeat heartbeat)
    {
        // Match flight-split vehicle selection: ArduPilot only, single sys/comp stream.
        if (heartbeat.Autopilot != Autopilot.ArduPilot)
        {
            return;
        }

        var source = (record.MavPacket.SysId, record.MavPacket.CompId);
        if (_armSource is null)
        {
            _armSource = source;
        }
        else if (_armSource != source)
        {
            return;
        }

        AdvanceTo(record.Trail / 1_000_000UL);

        var armed = heartbeat.BaseMode.HasFlag(MavModeFlag.SafetyArmed);
        if (_wasArmed != true && armed)
        {
            _armedFromUs = record.Trail;
        }
        else if (_wasArmed == true && !armed)
        {
            _armedFromUs = null;
        }

        _wasArmed = armed;
        _armed = armed;
    }

    /// <summary>Flush the final second and return derived samples (first ms of each second).</summary>
    public IReadOnlyList<MavMessageDto> TakeSamples()
    {
        if (_currentSecond.HasValue)
        {
            FinalizeCurrentSecond();
        }

        return _samples;
    }

    private void AdvanceTo(ulong second)
    {
        if (!_currentSecond.HasValue)
        {
            _currentSecond = second;
            return;
        }

        while (_currentSecond.Value < second)
        {
            FinalizeCurrentSecond();
            _currentSecond++;
            _secondReceived = 0;
            _secondLost = 0;
        }
    }

    private void FinalizeCurrentSecond()
    {
        if (!_currentSecond.HasValue)
        {
            return;
        }

        _window.Enqueue((_secondReceived, _secondLost));
        while (_window.Count > WindowSeconds)
        {
            _window.Dequeue();
        }

        var received = 0;
        var lost = 0;
        foreach (var (rx, lx) in _window)
        {
            received += rx;
            lost += lx;
        }

        if (received + lost == 0)
        {
            return;
        }

        var quality = received / (received + (double)lost) * 100.0;
        if (quality > 100.0)
        {
            quality = 100.0;
        }

        var firstMsUs = _currentSecond.Value * 1_000_000UL;
        double timeSinceArmSec = 0;
        if (_armed && _armedFromUs.HasValue)
        {
            var deltaUs = (long)firstMsUs - (long)_armedFromUs.Value;
            timeSinceArmSec = deltaUs > 0 ? deltaUs / 1_000_000.0 : 0;
        }

        _samples.Add(new MavMessageDto
        {
            Type = "derived",
            MessageId = DerivedMessageId,
            TimeUtc = TlogTime.ToUtcIso(firstMsUs),
            Data = new DerivedData
            {
                LinkQualityGcs = quality,
                TimeSinceArmSec = timeSinceArmSec
            }
        });
    }
}
