using System;
using System.Collections.Generic;
using System.Linq;
using LibreHardwareMonitor.Hardware;

namespace SSS.Core
{
    public static class Extensions
    {
        public static bool IsEnabled(this IEnumerable<MetricConfig> metrics, MetricKey key)
        {
            return metrics.Any(m => m.Key == key && m.Enabled);
        }

        public static void ApplyCustomLabels(this IEnumerable<MetricConfig> config, IEnumerable<iMetric> metrics)
        {
            var configDict = config.Where(m => !string.IsNullOrEmpty(m.Label)).ToDictionary(m => m.Key);
            if (configDict.Count == 0) return;

            foreach (var metric in metrics)
            {
                if (configDict.TryGetValue(metric.Key, out var mc) && !string.IsNullOrEmpty(mc.Label))
                {
                    metric.CustomLabel = mc.Label;
                }
            }
        }

        public static HardwareType[] GetHardwareTypes(this MonitorType type)
        {
            switch (type)
            {
                case MonitorType.CPU:
                    return [HardwareType.Cpu];

                case MonitorType.RAM:
                    return [HardwareType.Memory];

                case MonitorType.GPU:
                    return [HardwareType.GpuNvidia, HardwareType.GpuAmd];

                default:
                    throw new ArgumentException("Invalid MonitorType.");
            }
        }

        public static string GetDescription(this MonitorType type)
        {
            switch (type)
            {
                case MonitorType.CPU:
                    return Strings.CPU;

                case MonitorType.RAM:
                    return Strings.RAM;

                case MonitorType.GPU:
                    return Strings.GPU;

                case MonitorType.HD:
                    return Strings.Drives;

                case MonitorType.Network:
                    return Strings.Network;

                case MonitorType.Time:
                    return Strings.Time;

                case MonitorType.Window:
                    return Strings.Window;

                default:
                    return "???";
            }
        }

        public static string GetFullName(this MetricKey key)
        {
            switch (key)
            {
                case MetricKey.CPUClock:
                    return Strings.CPUClock;

                case MetricKey.CPUTemp:
                    return Strings.CPUTemp;

                case MetricKey.CPUVoltage:
                    return Strings.CPUVoltage;

                case MetricKey.CPUFan:
                    return Strings.CPUFan;

                case MetricKey.CPULoad:
                    return Strings.CPULoad;

                case MetricKey.CPUCoreLoad:
                    return Strings.CPUCoreLoad;

                case MetricKey.RAMClock:
                    return Strings.RAMClock;

                case MetricKey.RAMVoltage:
                    return Strings.RAMVoltage;

                case MetricKey.RAMLoad:
                    return Strings.RAMLoad;

                case MetricKey.RAMUsed:
                    return Strings.RAMUsed;

                case MetricKey.RAMFree:
                    return Strings.RAMFree;

                case MetricKey.GPUCoreClock:
                    return Strings.GPUCoreClock;

                case MetricKey.GPUVRAMClock:
                    return Strings.GPUVRAMClock;

                case MetricKey.GPUCoreLoad:
                    return Strings.GPUCoreLoad;

                case MetricKey.GPUVRAMLoad:
                    return Strings.GPUVRAMLoad;

                case MetricKey.GPUVoltage:
                    return Strings.GPUVoltage;

                case MetricKey.GPUTemp:
                    return Strings.GPUTemp;

                case MetricKey.GPUFan:
                    return Strings.GPUFan;

                case MetricKey.NetworkIP:
                    return Strings.NetworkIP;

                case MetricKey.NetworkExtIP:
                    return Strings.NetworkExtIP;

                case MetricKey.NetworkIn:
                    return Strings.NetworkIn;

                case MetricKey.NetworkOut:
                    return Strings.NetworkOut;

                case MetricKey.DriveLoadBar:
                    return Strings.DriveLoadBar;

                case MetricKey.DriveLoad:
                    return Strings.DriveLoad;

                case MetricKey.DriveUsed:
                    return Strings.DriveUsed;

                case MetricKey.DriveFree:
                    return Strings.DriveFree;

                case MetricKey.DriveRead:
                    return Strings.DriveRead;

                case MetricKey.DriveWrite:
                    return Strings.DriveWrite;

                case MetricKey.Time:
                    return Strings.Time;

                case MetricKey.Date:
                    return Strings.Date;

                default:
                    return "Unknown";
            }
        }

        public static string GetLabel(this MetricKey key)
        {
            switch (key)
            {
                case MetricKey.CPUClock:
                    return Strings.CPUClockLabel;

                case MetricKey.CPUTemp:
                    return Strings.CPUTempLabel;

                case MetricKey.CPUVoltage:
                    return Strings.CPUVoltageLabel;

                case MetricKey.CPUFan:
                    return Strings.CPUFanLabel;

                case MetricKey.CPULoad:
                    return Strings.CPULoadLabel;

                case MetricKey.CPUCoreLoad:
                    return Strings.CPUCoreLoadLabel;

                case MetricKey.RAMClock:
                    return Strings.RAMClockLabel;

                case MetricKey.RAMVoltage:
                    return Strings.RAMVoltageLabel;

                case MetricKey.RAMLoad:
                    return Strings.RAMLoadLabel;

                case MetricKey.RAMUsed:
                    return Strings.RAMUsedLabel;

                case MetricKey.RAMFree:
                    return Strings.RAMFreeLabel;

                case MetricKey.GPUCoreClock:
                    return Strings.GPUCoreClockLabel;

                case MetricKey.GPUVRAMClock:
                    return Strings.GPUVRAMClockLabel;

                case MetricKey.GPUCoreLoad:
                    return Strings.GPUCoreLoadLabel;

                case MetricKey.GPUVRAMLoad:
                    return Strings.GPUVRAMLoadLabel;

                case MetricKey.GPUVoltage:
                    return Strings.GPUVoltageLabel;

                case MetricKey.GPUTemp:
                    return Strings.GPUTempLabel;

                case MetricKey.GPUFan:
                    return Strings.GPUFanLabel;

                case MetricKey.NetworkIP:
                    return Strings.NetworkIPLabel;

                case MetricKey.NetworkExtIP:
                    return Strings.NetworkExtIPLabel;

                case MetricKey.NetworkIn:
                    return Strings.NetworkInLabel;

                case MetricKey.NetworkOut:
                    return Strings.NetworkOutLabel;

                case MetricKey.DriveLoadBar:
                    return Strings.DriveLoadBarLabel;

                case MetricKey.DriveLoad:
                    return Strings.DriveLoadLabel;

                case MetricKey.DriveUsed:
                    return Strings.DriveUsedLabel;

                case MetricKey.DriveFree:
                    return Strings.DriveFreeLabel;

                case MetricKey.DriveRead:
                    return Strings.DriveReadLabel;

                case MetricKey.DriveWrite:
                    return Strings.DriveWriteLabel;

                case MetricKey.Time:
                    return Strings.TimeLabel;

                case MetricKey.Date:
                    return Strings.DateLabel;

                default:
                    return "Unknown";
            }
        }

        public static string GetAppend(this DataType type)
        {
            switch (type)
            {
                case DataType.Bit:
                    return " b";

                case DataType.Kilobit:
                    return " kb";

                case DataType.Megabit:
                    return " mb";

                case DataType.Gigabit:
                    return " gb";

                case DataType.Byte:
                    return " B";

                case DataType.Kilobyte:
                    return " KB";

                case DataType.Megabyte:
                    return " MB";

                case DataType.Gigabyte:
                    return " GB";

                case DataType.bps:
                    return " bps";

                case DataType.kbps:
                    return " kbps";

                case DataType.Mbps:
                    return " Mbps";

                case DataType.Gbps:
                    return " Gbps";

                case DataType.Bps:
                    return " B/s";

                case DataType.kBps:
                    return " kB/s";

                case DataType.MBps:
                    return " MB/s";

                case DataType.GBps:
                    return " GB/s";

                case DataType.MHz:
                    return " MHz";

                case DataType.GHz:
                    return " GHz";

                case DataType.Voltage:
                    return " V";

                case DataType.Percent:
                    return "%";

                case DataType.RPM:
                    return " RPM";

                case DataType.Celcius:
                    return " C";

                case DataType.Fahrenheit:
                    return " F";

                case DataType.IP:
                    return string.Empty;

                case DataType.Dynamic:
                    return string.Empty;

                default:
                    throw new ArgumentException("Invalid DataType.");
            }
        }

        public static double Round(this double value, bool doRound)
        {
            if (!doRound)
            {
                return value;
            }

            return Math.Round(value);
        }
    }
}
