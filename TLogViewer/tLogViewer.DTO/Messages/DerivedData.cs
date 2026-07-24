namespace tLogViewer.DTO.Messages;

/// <summary>Synthetic DERIVED (998) payload — values computed from the log, not wire MAVLink.</summary>
public sealed class DerivedData
{
    public double LinkQualityGcs { get; init; }
    public double TimeSinceArmSec { get; init; }
    public double? AzToMav { get; init; }
    public double? DistToHome { get; init; }
    public double? Watts { get; init; }
    public double? VerticalSpeedFpm { get; init; }
    public double? Vlen { get; init; }
    public double? GlideRatio { get; init; }
    public double? BerError { get; init; }
    public double? SatcountB { get; init; }
    public double? AltD100 { get; init; }
    public double? AltD1000 { get; init; }
    public double? TargetAlt { get; init; }
    public double? TargetAirspeed { get; init; }
    public double? TargetAltD100 { get; init; }
    public double? TurnRate { get; init; }
    public double? TurnG { get; init; }
    public double? Radius { get; init; }
    public double? Tot { get; init; }
    public double? Toh { get; init; }
    public double? ElToMav { get; init; }
    public double? DistTraveled { get; init; }
    public double? LocalSnrDb { get; init; }
    public double? RemoteSnrDb { get; init; }
    public double? DistRssiRemain { get; init; }
    public double? Qnh { get; init; }
    public double? CritAoa { get; init; }
    public double? BatteryMahPerKm { get; init; }
    public double? BatteryKmLeft { get; init; }
    public double? HorizonDist { get; init; }
}
