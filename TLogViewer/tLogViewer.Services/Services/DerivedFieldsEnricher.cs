namespace tLogViewer.Services.Services;

/// <summary>
/// Fills DERIVED (998) and other calculated HUD fields on every flight millisecond.
/// </summary>
public static class DerivedFieldsEnricher
{
    private static readonly string LinkQualityKey = FlightFieldIds.LinkQualityGcs;
    private static readonly string TimeSinceArmKey = FlightFieldIds.TimeSinceArmSec;
    private static readonly string AzToMavKey = FlightFieldIds.AzToMav;
    private static readonly string DistToHomeKey = FlightFieldIds.DistToHome;
    private static readonly string ArmedKey = FlightFieldIds.Armed;
    private static readonly string HomeLatKey = FlightFieldIds.HomeLatitudeDeg;
    private static readonly string HomeLonKey = FlightFieldIds.HomeLongitudeDeg;
    private static readonly string HomeAltKey = FlightFieldIds.HomeAltitudeM;
    private static readonly string PlaneLatKey = FlightFieldIds.AliasLat;
    private static readonly string PlaneLonKey = FlightFieldIds.AliasLon;
    private static readonly string BatteryVoltageKey = FlightFieldIds.BatteryVoltageV;
    private static readonly string BatteryCurrentKey = "1_001";
    private static readonly string BatteryRemainingKey = "1_002";
    private static readonly string VerticalSpeedKey = FlightFieldIds.VerticalSpeed;
    private static readonly string VxKey = FlightFieldIds.Vx;
    private static readonly string VyKey = FlightFieldIds.Vy;
    private static readonly string VzKey = FlightFieldIds.Vz;
    private static readonly string YawKey = FlightFieldIds.AliasYaw;
    private static readonly string TargetBearingKey = FlightFieldIds.NavTargetBearing;
    private static readonly string SatcountKey = "24_009";
    private static readonly string Satcount2Key = FlightFieldIds.Satcount2;
    private static readonly string RelativeAltKey = "33_006";
    private static readonly string AslAltKey = "33_001";
    private static readonly string AltErrorKey = "62_001";
    private static readonly string AspdErrorKey = "62_002";
    private static readonly string AirspeedKey = "74_001";
    private static readonly string GroundspeedKey = "74_004";
    private static readonly string WpDistKey = FlightFieldIds.NavWpDist;
    private static readonly string RollKey = "30_003";
    private static readonly string RssiKey = "166_005";
    private static readonly string NoiseKey = "166_002";
    private static readonly string RemRssiKey = "166_004";
    private static readonly string RemNoiseKey = "166_003";
    private static readonly string PressAbsKey = FlightFieldIds.PressAbs;
    private static readonly string BatteryUsedMahKey = FlightFieldIds.BatteryUsedMah;

    private const double EarthRadiusM = 6_371_000;
    private const double Gravity = 9.80665;
    private const double CritAoaDeg = 25.0;
    private const double MsToFpm = 196.8503937;

    public static void ForwardFill(Dictionary<long, Dictionary<string, object>> byMillisecond)
    {
        if (byMillisecond.Count == 0)
        {
            return;
        }

        object? lastLinkQuality = null;
        bool? armed = null;
        long? armedFromMs = null;
        double? homeLat = null;
        double? homeLon = null;
        double? homeAlt = null;
        double? prevTargetAlt = null;
        double? prevTargetAirspeed = null;
        double? lastLocalSnr = null;
        double? lastRemoteSnr = null;
        double? lastLat = null;
        double? lastLon = null;
        double distTraveled = 0;

        foreach (var ms in byMillisecond.Keys.OrderBy(static key => key))
        {
            var atMs = byMillisecond[ms];

            if (atMs.TryGetValue(LinkQualityKey, out var linkQuality))
            {
                lastLinkQuality = linkQuality;
            }
            else if (lastLinkQuality is not null)
            {
                atMs[LinkQualityKey] = lastLinkQuality;
            }

            if (atMs.TryGetValue(ArmedKey, out var armedObj) && armedObj is bool isArmed)
            {
                if (isArmed && armed != true)
                {
                    armedFromMs = ms;
                }
                else if (!isArmed)
                {
                    armedFromMs = null;
                }

                armed = isArmed;
            }

            double timeSinceArmSec = 0;
            if (armed == true && armedFromMs.HasValue)
            {
                timeSinceArmSec = Math.Max(0, (ms - armedFromMs.Value) / 1000.0);
            }

            atMs[TimeSinceArmKey] = timeSinceArmSec;

            if (TryReadCoordinate(atMs, HomeLatKey, HomeLonKey, out var nextHomeLat, out var nextHomeLon))
            {
                homeLat = nextHomeLat;
                homeLon = nextHomeLon;
            }

            if (TryAsDouble(atMs, HomeAltKey, out var nextHomeAlt))
            {
                homeAlt = nextHomeAlt;
            }

            if (homeLat is not null
                && homeLon is not null
                && TryReadCoordinate(atMs, PlaneLatKey, PlaneLonKey, out var planeLat, out var planeLon))
            {
                var distHome = HaversineDistanceM(homeLat.Value, homeLon.Value, planeLat, planeLon);
                atMs[DistToHomeKey] = distHome;
                atMs[AzToMavKey] = InitialBearingDeg(homeLat.Value, homeLon.Value, planeLat, planeLon);

                if (lastLat is not null && lastLon is not null)
                {
                    distTraveled += HaversineDistanceM(lastLat.Value, lastLon.Value, planeLat, planeLon);
                }

                lastLat = planeLat;
                lastLon = planeLon;
                atMs[FlightFieldIds.DistTraveled] = distTraveled;
            }

            WritePowerAndSpeed(atMs);
            WriteAttitudeDerived(atMs);
            WriteTargets(atMs, ref prevTargetAlt, ref prevTargetAirspeed);
            WriteHomeTiming(atMs, homeAlt);
            WriteRadioSnr(atMs, ref lastLocalSnr, ref lastRemoteSnr);
            WriteQnh(atMs);
            WriteBatteryEconomy(atMs, distTraveled);

            atMs[FlightFieldIds.CritAoa] = CritAoaDeg;

            if (TryAsDouble(atMs, SatcountKey, out var sats)
                && TryAsDouble(atMs, Satcount2Key, out var sats2))
            {
                atMs[FlightFieldIds.SatcountB] = sats + sats2;
            }
        }
    }

    private static void WritePowerAndSpeed(Dictionary<string, object> atMs)
    {
        if (TryAsDouble(atMs, BatteryVoltageKey, out var volts)
            && TryAsDouble(atMs, BatteryCurrentKey, out var amps))
        {
            atMs[FlightFieldIds.Watts] = volts * amps;
        }

        if (TryAsDouble(atMs, VerticalSpeedKey, out var vs))
        {
            atMs[FlightFieldIds.VerticalSpeedFpm] = vs * -MsToFpm;
        }
        else if (TryAsDouble(atMs, VzKey, out var vz))
        {
            atMs[FlightFieldIds.VerticalSpeedFpm] = vz * -MsToFpm;
        }

        if (TryAsDouble(atMs, VxKey, out var vx)
            && TryAsDouble(atMs, VyKey, out var vy)
            && TryAsDouble(atMs, VzKey, out var vz2))
        {
            atMs[FlightFieldIds.Vlen] = Math.Sqrt(vx * vx + vy * vy + vz2 * vz2);
            var horiz = Math.Sqrt(vx * vx + vy * vy);
            if (Math.Abs(vz2) > 1e-6)
            {
                atMs[FlightFieldIds.GlideRatio] = horiz / Math.Abs(vz2);
            }
        }

        if (TryAsDouble(atMs, RelativeAltKey, out var alt))
        {
            atMs[FlightFieldIds.AltD100] = Math.Floor(alt / 100.0) % 10;
            atMs[FlightFieldIds.AltD1000] = Math.Floor(alt / 1000.0) % 10;
            if (alt >= 0)
            {
                atMs[FlightFieldIds.HorizonDist] = 3570.0 * Math.Sqrt(alt);
            }
        }
    }

    private static void WriteAttitudeDerived(Dictionary<string, object> atMs)
    {
        if (TryAsDouble(atMs, TargetBearingKey, out var targetBearing)
            && TryAsDouble(atMs, YawKey, out var yaw))
        {
            atMs[FlightFieldIds.BerError] = NormalizeHeading(targetBearing - yaw);
        }

        if (!TryAsDouble(atMs, RollKey, out var rollDeg))
        {
            return;
        }

        var rollRad = rollDeg * Math.PI / 180.0;
        var cosRoll = Math.Cos(rollRad);
        if (Math.Abs(cosRoll) > 1e-6)
        {
            atMs[FlightFieldIds.TurnG] = 1.0 / cosRoll;
        }

        if (!TryAsDouble(atMs, GroundspeedKey, out var gs) || gs <= 1)
        {
            atMs[FlightFieldIds.TurnRate] = 0.0;
            atMs[FlightFieldIds.Radius] = 0.0;
            return;
        }

        atMs[FlightFieldIds.TurnRate] = rollRad * Gravity / gs;
        var tanRoll = Math.Tan(rollRad);
        atMs[FlightFieldIds.Radius] = Math.Abs(tanRoll) < 1e-6
            ? 0.0
            : gs * gs / (Gravity * tanRoll);
    }

    private static void WriteTargets(
        Dictionary<string, object> atMs,
        ref double? prevTargetAlt,
        ref double? prevTargetAirspeed)
    {
        if (TryAsDouble(atMs, RelativeAltKey, out var alt)
            && TryAsDouble(atMs, AltErrorKey, out var altError))
        {
            var raw = Math.Round(alt + altError);
            var smoothed = prevTargetAlt is { } prev ? 0.5 * prev + 0.5 * raw : raw;
            prevTargetAlt = smoothed;
            atMs[FlightFieldIds.TargetAlt] = smoothed;
            atMs[FlightFieldIds.TargetAltD100] = Math.Floor(smoothed / 100.0) % 10;
        }

        if (TryAsDouble(atMs, AirspeedKey, out var aspd)
            && TryAsDouble(atMs, AspdErrorKey, out var aspdError))
        {
            var raw = Math.Round(aspd + aspdError);
            var smoothed = prevTargetAirspeed is { } prev ? 0.5 * prev + 0.5 * raw : raw;
            prevTargetAirspeed = smoothed;
            atMs[FlightFieldIds.TargetAirspeed] = smoothed;
        }
    }

    private static void WriteHomeTiming(Dictionary<string, object> atMs, double? homeAlt)
    {
        if (!TryAsDouble(atMs, GroundspeedKey, out var gs) || gs <= 0.1)
        {
            return;
        }

        if (TryAsDouble(atMs, WpDistKey, out var wpDist))
        {
            atMs[FlightFieldIds.Tot] = wpDist / gs;
        }

        if (TryAsDouble(atMs, DistToHomeKey, out var distHome))
        {
            atMs[FlightFieldIds.Toh] = distHome / gs;

            if (homeAlt is { } hAlt
                && TryAsDouble(atMs, AslAltKey, out var asl)
                && distHome > 0.5)
            {
                atMs[FlightFieldIds.ElToMav] = Math.Atan((asl - hAlt) / distHome) * 180.0 / Math.PI;
            }
        }
    }

    private static void WriteRadioSnr(
        Dictionary<string, object> atMs,
        ref double? lastLocalSnr,
        ref double? lastRemoteSnr)
    {
        if (TryAsDouble(atMs, RssiKey, out var rssi) && TryAsDouble(atMs, NoiseKey, out var noise))
        {
            var sample = (rssi - noise) / 1.9;
            lastLocalSnr = lastLocalSnr is { } prev ? 0.7 * prev + 0.3 * sample : sample;
            atMs[FlightFieldIds.LocalSnrDb] = lastLocalSnr.Value;
        }

        if (TryAsDouble(atMs, RemRssiKey, out var remRssi)
            && TryAsDouble(atMs, RemNoiseKey, out var remNoise))
        {
            var sample = (remRssi - remNoise) / 1.9;
            lastRemoteSnr = lastRemoteSnr is { } prev ? 0.7 * prev + 0.3 * sample : sample;
            atMs[FlightFieldIds.RemoteSnrDb] = lastRemoteSnr.Value;
        }

        if (lastLocalSnr is { } local
            && lastRemoteSnr is { } remote
            && TryAsDouble(atMs, DistToHomeKey, out var distHome)
            && distHome > 0)
        {
            var minSnr = Math.Min(local, remote);
            atMs[FlightFieldIds.DistRssiRemain] = distHome * Math.Pow(2, (minSnr - 5) / 6.0);
        }
    }

    private static void WriteQnh(Dictionary<string, object> atMs)
    {
        if (!TryAsDouble(atMs, PressAbsKey, out var pressAbs)
            || !TryAsDouble(atMs, AslAltKey, out var asl)
            || pressAbs <= 0)
        {
            return;
        }

        // ISA-style QNH approximation used by Mission Planner (hPa).
        var qnh = pressAbs / Math.Pow(1.0 - asl / 44330.77, 5.255883);
        if (double.IsFinite(qnh))
        {
            atMs[FlightFieldIds.Qnh] = qnh;
        }
    }

    private static void WriteBatteryEconomy(Dictionary<string, object> atMs, double distTraveledM)
    {
        if (!TryAsDouble(atMs, BatteryUsedMahKey, out var usedMah) || usedMah <= 0)
        {
            return;
        }

        var distKm = distTraveledM / 1000.0;
        if (distKm > 0.01)
        {
            var mahPerKm = usedMah / distKm;
            atMs[FlightFieldIds.BatteryMahPerKm] = mahPerKm;

            if (TryAsDouble(atMs, BatteryRemainingKey, out var remaining)
                && remaining is > 0 and < 100
                && mahPerKm > 0)
            {
                var totalEst = (100.0 / (100.0 - remaining)) * usedMah;
                atMs[FlightFieldIds.BatteryKmLeft] = (totalEst - usedMah) / mahPerKm;
            }
        }
    }

    private static bool TryReadCoordinate(
        IReadOnlyDictionary<string, object> fields,
        string latKey,
        string lonKey,
        out double latitudeDeg,
        out double longitudeDeg)
    {
        latitudeDeg = 0;
        longitudeDeg = 0;

        if (!fields.TryGetValue(latKey, out var latObj)
            || !fields.TryGetValue(lonKey, out var lonObj)
            || !TryAsDouble(latObj, out latitudeDeg)
            || !TryAsDouble(lonObj, out longitudeDeg))
        {
            return false;
        }

        return !IsInvalidCoordinate(latitudeDeg, longitudeDeg);
    }

    private static bool TryAsDouble(IReadOnlyDictionary<string, object> fields, string key, out double result)
    {
        if (fields.TryGetValue(key, out var value))
        {
            return TryAsDouble(value, out result);
        }

        result = 0;
        return false;
    }

    private static double HaversineDistanceM(
        double lat1Deg,
        double lon1Deg,
        double lat2Deg,
        double lon2Deg)
    {
        var lat1 = lat1Deg * Math.PI / 180.0;
        var lon1 = lon1Deg * Math.PI / 180.0;
        var lat2 = lat2Deg * Math.PI / 180.0;
        var lon2 = lon2Deg * Math.PI / 180.0;
        var dLat = lat2 - lat1;
        var dLon = lon2 - lon1;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(Math.Max(0, 1 - a)));
        return EarthRadiusM * c;
    }

    private static double InitialBearingDeg(
        double lat1Deg,
        double lon1Deg,
        double lat2Deg,
        double lon2Deg)
    {
        var lat1 = lat1Deg * Math.PI / 180.0;
        var lat2 = lat2Deg * Math.PI / 180.0;
        var dLon = (lon2Deg - lon1Deg) * Math.PI / 180.0;

        var y = Math.Sin(dLon) * Math.Cos(lat2);
        var x = Math.Cos(lat1) * Math.Sin(lat2)
            - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
        return NormalizeHeading(Math.Atan2(y, x) * 180.0 / Math.PI);
    }

    private static double NormalizeHeading(double degrees)
    {
        var normalized = degrees % 360;
        if (normalized < 0)
        {
            normalized += 360;
        }

        return normalized;
    }

    private static bool IsInvalidCoordinate(double latitudeDeg, double longitudeDeg) =>
        Math.Abs(latitudeDeg) < 1e-9 && Math.Abs(longitudeDeg) < 1e-9;

    private static bool TryAsDouble(object value, out double result)
    {
        switch (value)
        {
            case double d when double.IsFinite(d):
                result = d;
                return true;
            case float f when float.IsFinite(f):
                result = f;
                return true;
            case int i:
                result = i;
                return true;
            case long l:
                result = l;
                return true;
            case ushort us:
                result = us;
                return true;
            case short s:
                result = s;
                return true;
            case byte b:
                result = b;
                return true;
            case decimal m:
                result = (double)m;
                return true;
            default:
                result = 0;
                return false;
        }
    }
}
