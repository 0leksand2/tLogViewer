namespace tLogViewer.Core.Enums.SysStatus
{
    [Flags]
    public enum MavSysSensor : uint
    {
        Gyro = 1 << 0,
        Accelerometer = 1 << 1,
        Magnetometer = 1 << 2,
        AbsolutePressure = 1 << 3,
        DifferentialPressure = 1 << 4,
        GPS = 1 << 5,
        OpticalFlow = 1 << 6,
        VisionPosition = 1 << 7,
        LaserPosition = 1 << 8,
        RCReceiver = 1 << 10,
        AHRS = 1 << 11,
        Terrain = 1 << 22,
        Geofence = 1 << 20
    }
}
