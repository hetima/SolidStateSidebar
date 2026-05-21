using System;
using System.Collections.Generic;
using System.Linq;
using LibreHardwareMonitor.Hardware;
using SSS.Core;

namespace SSS.Module.RamMonitor
{
    public partial class RAMMonitor : OHMMonitorBase
    {
        private RAMMonitor(string id, string name, IHardware hardware, IHardware board, MetricConfig[] metrics, bool roundAll) : base(id, name, hardware, false)
        {
            InitRAM(board, metrics, roundAll);
        }

        public static RAMMonitor[] GetInstances(Data data, IHardware board, IHardware[] hardware)
        {
            var deduped = data.Hardware!
                .GroupBy(h => h.ID)
                .Select(g => g.First())
                .ToArray();

            return CreateInstances(deduped, hardware,
                (c, hw) => new RAMMonitor(c.ID!, c.Name ?? c.ActualName!, hw, board, data.Metrics!.ToArray(), data.RoundAll));
        }

        private void InitRAM(IHardware board, MetricConfig[] metrics, bool roundAll)
        {
            List<OHMMetric> _sensorList = new List<Core.OHMMetric>();

            if (metrics.IsEnabled(MetricKey.RAMClock))
            {
                ISensor? _ramClock = FindSensor(Hardware!.Sensors, s => s.SensorType == SensorType.Clock);

                if (_ramClock != null)
                {
                    _sensorList.Add(new OHMMetric(_ramClock, MetricKey.RAMClock, DataType.MHz, null, true));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMVoltage))
            {
                ISensor? _voltage = FindSensorWithFallback(
                    board?.Sensors, Hardware!.Sensors,
                    s => s.SensorType == SensorType.Voltage && s.Name.Contains("RAM"),
                    s => s.SensorType == SensorType.Voltage);

                if (_voltage != null)
                {
                    _sensorList.Add(new OHMMetric(_voltage, MetricKey.RAMVoltage, DataType.Voltage, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMLoad))
            {
                ISensor? _loadSensor = FindSensor(Hardware!.Sensors, s => s.SensorType == SensorType.Load && s.Index == 0);

                if (_loadSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_loadSensor, MetricKey.RAMLoad, DataType.Percent, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMUsed))
            {
                ISensor? _usedSensor = FindSensor(Hardware!.Sensors, s => s.SensorType == SensorType.Data && s.Index == 0);

                if (_usedSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_usedSensor, MetricKey.RAMUsed, DataType.Gigabyte, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMFree))
            {
                ISensor? _freeSensor = FindSensor(Hardware!.Sensors, s => s.SensorType == SensorType.Data && s.Index == 1);

                if (_freeSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_freeSensor, MetricKey.RAMFree, DataType.Gigabyte, null, roundAll));
                }
            }

            Metrics = _sensorList.ToArray();
            metrics.ApplyCustomLabels(Metrics);
        }
    }
}
