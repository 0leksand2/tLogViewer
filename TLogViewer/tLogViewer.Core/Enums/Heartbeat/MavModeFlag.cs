namespace tLogViewer.Core.Enums.Heartbeat
{
    [Flags]
    public enum MavModeFlag : byte
    {
        None = 0,

        CustomModeEnabled = 1,      // 0x01
        TestEnabled = 2,            // 0x02
        AutoEnabled = 4,            // 0x04
        GuidedEnabled = 8,          // 0x08
        StabilizeEnabled = 16,      // 0x10
        HilEnabled = 32,            // 0x20
        ManualInputEnabled = 64,    // 0x40
        SafetyArmed = 128           // 0x80
    }
}
