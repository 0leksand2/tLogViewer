using tLogViewer.Reader.Services;

foreach (var record in LogReader.ReadTLog("D:\\projects\\tLogViewer\\data\\2026-06-04 18-42-04.tlog"))
{
    Console.WriteLine($"Trail={record.Trail} Packet={record.MavPacket.Seq}, Type={record.MavPacket.MsgId}");
}