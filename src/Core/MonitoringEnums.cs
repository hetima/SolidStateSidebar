using System;

namespace SSS.Core
{
    [Serializable]
    public enum MonitorType : byte
    {
        CPU,
        RAM,
        GPU,
        HD,
        Network,
        Time,
        Window,
        Claude,
        Codex
    }

    [Serializable]
    public enum MetricKey : byte
    {
        CPUClock = 0,
        CPUTemp = 1,
        CPUVoltage = 2,
        CPUFan = 3,
        CPULoad = 4,
        CPUCoreLoad = 5,

        RAMClock = 6,
        RAMVoltage = 7,
        RAMLoad = 8,
        RAMUsed = 9,
        RAMFree = 10,

        GPUCoreClock = 11,
        GPUVRAMClock = 12,
        GPUCoreLoad = 13,
        GPUVRAMLoad = 14,
        GPUVoltage = 15,
        GPUTemp = 16,
        GPUFan = 17,

        NetworkIP = 26,
        NetworkExtIP = 27,
        NetworkIn = 18,
        NetworkOut = 19,

        DriveLoadBar = 20,
        DriveLoad = 21,
        DriveUsed = 22,
        DriveFree = 23,
        DriveRead = 24,
        DriveWrite = 25,

        Time = 28,
        Date = 29,

        WindowTitle = 30,

        Claude5h = 31,
        Claude1w = 32,
        Codex5h  = 33,
        Codex1w  = 34,
        CodexCredits = 35
    }

    public enum ResetTimeDisplay : byte
    {
        Countdown,
        Absolute
    }

    public enum AutoRefreshInterval : byte
    {
        Manual,
        OneMin,
        FiveMin,
        TenMin
    }

    public enum TextAlign : byte
    {
        Left,
        Right
    }

    public enum SectionHeaderStyle : byte
    {
        Default,
        Small,
        None,
        NoIcon
    }

    public enum DataType : byte
    {
        Dynamic,
        Bit,
        Kilobit,
        Megabit,
        Gigabit,
        Byte,
        Kilobyte,
        Megabyte,
        Gigabyte,
        bps,
        kbps,
        Mbps,
        Gbps,
        Bps,
        kBps,
        MBps,
        GBps,
        MHz,
        GHz,
        Voltage,
        Percent,
        RPM,
        Celcius,
        Fahrenheit,
        IP
    }
}
