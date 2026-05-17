using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LibreHardwareMonitor.Hardware;

namespace SSS.Core
{
    public partial class OHMMonitor : BaseMonitor
    {
        public OHMMonitor(MonitorType type, string id, string name, IHardware hardware, IHardware board, MetricConfig[] metrics, bool showHardwareNames, bool roundAll, bool allCoreClocks, bool useGHz, bool useFahrenheit, int tempAlert) : base(id, name, showHardwareNames)
        {
            _hardware = hardware;

            UpdateHardware();

            switch (type)
            {
                case MonitorType.CPU:
                    InitCPU(board, metrics, roundAll, allCoreClocks, useGHz, useFahrenheit, tempAlert);
                    break;

                case MonitorType.RAM:
                    InitRAM(board, metrics, roundAll);
                    break;

                case MonitorType.GPU:
                    InitGPU(metrics, roundAll, useGHz, useFahrenheit, tempAlert);
                    break;

                default:
                    throw new ArgumentException("Invalid MonitorType.");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _hardware = null;
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }

        ~OHMMonitor()
        {
            Dispose(false);
        }

        public static iMonitor[] GetInstances(HardwareConfig[] hardwareConfig, MetricConfig[] metrics, MonitorType type, IHardware board, IHardware[] hardware, bool showHardwareNames, bool roundAll, bool allCoreClocks, bool useGHz, bool useFahrenheit, int tempAlert)
        {
            return (
                from hw in hardware
                join c in hardwareConfig on hw.Identifier.ToString() equals c.ID into merged
                from n in merged.DefaultIfEmpty(new HardwareConfig() { ID = hw.Identifier.ToString(), Name = hw.Name, ActualName = hw.Name }).Select(n => { if (n.ActualName != hw.Name) { n.Name = n.ActualName = hw.Name; } return n; })
                where n.Enabled
                orderby n.Order descending, n.Name ascending
                select new OHMMonitor(type, n.ID!, n.Name ?? n.ActualName!, hw, board, metrics, showHardwareNames, roundAll, allCoreClocks, useGHz, useFahrenheit, tempAlert)
                ).ToArray();
        }

        public override void Update()
        {
            UpdateHardware();

            base.Update();
        }

        private void UpdateHardware()
        {
            _hardware!.Update();
        }

        private void InitCPU(IHardware board, MetricConfig[] metrics, bool roundAll, bool allCoreClocks, bool useGHz, bool useFahrenheit, double tempAlert)
        {
            List<OHMMetric> _sensorList = new List<OHMMetric>();

            if (metrics.IsEnabled(MetricKey.CPUClock))
            {
                var coreClocks = _hardware!.Sensors
                    .Where(s => s.SensorType == SensorType.Clock)
                    .Select(s => new
                    {
                        Match = CpuCoreClockRegex().Match(s.Name),
                        Sensor = s
                    })
                    .Where(s => s.Match.Success)
                    .Select(s => new
                    {
                        Index = int.Parse(s.Match.Groups[2].Value),
                        s.Sensor
                    })
                    .OrderBy(s => s.Index)
                    .ToList();

                if (coreClocks.Count > 0)
                {
                    if (allCoreClocks)
                    {
                        foreach (var coreClock in coreClocks)
                        {
                            _sensorList.Add(new OHMMetric(coreClock.Sensor, MetricKey.CPUClock, DataType.MHz, string.Format("{0} {1}", Strings.CPUCoreClockLabel, coreClock.Index - 1), !useGHz, 0, (useGHz ? MHzToGHz.Instance : null)));
                        }
                    }
                    else
                    {
                        ISensor? firstClock = coreClocks
                            .Select(s => s.Sensor)
                            .FirstOrDefault();

                        if (firstClock != null)
                        {
                            _sensorList.Add(new OHMMetric(firstClock, MetricKey.CPUClock, DataType.MHz, null, !useGHz, 0, (useGHz ? MHzToGHz.Instance : null)));
                        }
                    }
                }
            }

            if (metrics.IsEnabled(MetricKey.CPUVoltage))
            {
                ISensor? _voltage = FindSensorWithFallback(
                    board?.Sensors, _hardware!.Sensors,
                    s => s.SensorType == SensorType.Voltage && s.Name.Contains("CPU"),
                    s => s.SensorType == SensorType.Voltage);

                if (_voltage != null)
                {
                    _sensorList.Add(new OHMMetric(_voltage, MetricKey.CPUVoltage, DataType.Voltage, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.CPUTemp))
            {
                ISensor? _tempSensor = FindSensor(_hardware!.Sensors, s => s.SensorType == SensorType.Temperature && s.Name.Contains("CCDs Max (Tdie)")) // Check for AMD core chiplet dies (CCDs)
                    ?? FindSensorWithFallback(
                        board?.Sensors, _hardware!.Sensors,
                        s => s.SensorType == SensorType.Temperature && s.Name.Contains("CPU"),
                        s => s.SensorType == SensorType.Temperature && (s.Name == "CPU Package" || s.Name.Contains("Tdie")))
                    ?? FindSensor(_hardware!.Sensors, s => s.SensorType == SensorType.Temperature);

                if (_tempSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_tempSensor, MetricKey.CPUTemp, DataType.Celcius, null, roundAll, tempAlert, (useFahrenheit ? CelciusToFahrenheit.Instance : null)));
                }
            }

            if (metrics.IsEnabled(MetricKey.CPUFan))
            {
                static bool IsFanOrControl(ISensor s) => s.SensorType == SensorType.Fan || s.SensorType == SensorType.Control;

                ISensor? _fanSensor = FindSensorWithFallback(
                    board?.Sensors, _hardware!.Sensors,
                    s => IsFanOrControl(s) && s.Name.Contains("CPU"),
                    IsFanOrControl);

                if (_fanSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_fanSensor, MetricKey.CPUFan, DataType.RPM, null, roundAll));
                }
            }

            bool _loadEnabled = metrics.IsEnabled(MetricKey.CPULoad);
            bool _coreLoadEnabled = metrics.IsEnabled(MetricKey.CPUCoreLoad);

            if (_loadEnabled || _coreLoadEnabled)
            {
                ISensor[] _loadSensors = FindSensors(_hardware!.Sensors, s => s.SensorType == SensorType.Load);

                if (_loadSensors.Length > 0)
                {
                    if (_loadEnabled)
                    {
                        ISensor? _totalCPU = Array.Find(_loadSensors, s => s.Index == 0);

                        if (_totalCPU != null)
                        {
                            _sensorList.Add(new OHMMetric(_totalCPU, MetricKey.CPULoad, DataType.Percent, null, roundAll));
                        }
                    }

                    if (_coreLoadEnabled)
                    {
                        for (int i = 1; i <= _loadSensors.Max(s => s.Index); i++)
                        {
                            ISensor? _coreLoad = Array.Find(_loadSensors, s => s.Index == i);

                            if (_coreLoad != null)
                            {
                                _sensorList.Add(new OHMMetric(_coreLoad, MetricKey.CPUCoreLoad, DataType.Percent, string.Format("{0} {1}", Strings.CPUCoreLoadLabel, i - 1), roundAll));
                            }
                        }
                    }
                }
            }

            Metrics = _sensorList.ToArray();
        }

        public void InitRAM(IHardware board, MetricConfig[] metrics, bool roundAll)
        {
            List<OHMMetric> _sensorList = new List<OHMMetric>();

            if (metrics.IsEnabled(MetricKey.RAMClock))
            {
                ISensor? _ramClock = FindSensor(_hardware!.Sensors, s => s.SensorType == SensorType.Clock);

                if (_ramClock != null)
                {
                    _sensorList.Add(new OHMMetric(_ramClock, MetricKey.RAMClock, DataType.MHz, null, true));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMVoltage))
            {
                ISensor? _voltage = FindSensorWithFallback(
                    board?.Sensors, _hardware!.Sensors,
                    s => s.SensorType == SensorType.Voltage && s.Name.Contains("RAM"),
                    s => s.SensorType == SensorType.Voltage);

                if (_voltage != null)
                {
                    _sensorList.Add(new OHMMetric(_voltage, MetricKey.RAMVoltage, DataType.Voltage, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMLoad))
            {
                ISensor? _loadSensor = FindSensor(_hardware!.Sensors, s => s.SensorType == SensorType.Load && s.Index == 0);

                if (_loadSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_loadSensor, MetricKey.RAMLoad, DataType.Percent, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMUsed))
            {
                ISensor? _usedSensor = FindSensor(_hardware!.Sensors, s => s.SensorType == SensorType.Data && s.Index == 0);

                if (_usedSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_usedSensor, MetricKey.RAMUsed, DataType.Gigabyte, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMFree))
            {
                ISensor? _freeSensor = FindSensor(_hardware!.Sensors, s => s.SensorType == SensorType.Data && s.Index == 1);

                if (_freeSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_freeSensor, MetricKey.RAMFree, DataType.Gigabyte, null, roundAll));
                }
            }

            Metrics = _sensorList.ToArray();
        }

        public void InitGPU(MetricConfig[] metrics, bool roundAll, bool useGHz, bool useFahrenheit, double tempAlert)
        {
            List<iMetric> _sensorList = new List<iMetric>();

            if (metrics.IsEnabled(MetricKey.GPUCoreClock))
            {
                ISensor? _coreClock = FindSensor(_hardware!.Sensors, s => s.SensorType == SensorType.Clock && s.Name.Contains("Core"));

                if (_coreClock != null)
                {
                    _sensorList.Add(new OHMMetric(_coreClock, MetricKey.GPUCoreClock, DataType.MHz, null, !useGHz, 0, (useGHz ? MHzToGHz.Instance : null)));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUVRAMClock))
            {
                ISensor? _vramClock = FindSensor(_hardware!.Sensors, s => s.SensorType == SensorType.Clock && s.Name.Contains("Memory"));

                if (_vramClock != null)
                {
                    _sensorList.Add(new OHMMetric(_vramClock, MetricKey.GPUVRAMClock, DataType.MHz, null, !useGHz, 0, (useGHz ? MHzToGHz.Instance : null)));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUCoreLoad))
            {
                ISensor? _coreLoad = FindSensor(_hardware!.Sensors, s => s.SensorType == SensorType.Load && s.Name.Contains("Core")) ??
                    FindSensor(_hardware!.Sensors, s => s.SensorType == SensorType.Load && s.Index == 0);

                if (_coreLoad != null)
                {
                    _sensorList.Add(new OHMMetric(_coreLoad, MetricKey.GPUCoreLoad, DataType.Percent, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUVRAMLoad))
            {
                static bool IsMemoryData(ISensor s) => (s.SensorType == SensorType.Data || s.SensorType == SensorType.SmallData);

                ISensor? _memoryUsed = FindSensor(_hardware!.Sensors, s => IsMemoryData(s) && s.Name == "GPU Memory Used");
                ISensor? _memoryTotal = FindSensor(_hardware!.Sensors, s => IsMemoryData(s) && s.Name == "GPU Memory Total");

                if (_memoryUsed != null && _memoryTotal != null)
                {
                    _sensorList.Add(new GPUVRAMMLoadMetric(_memoryUsed, _memoryTotal, MetricKey.GPUVRAMLoad, DataType.Percent, null, roundAll));
                }
                else
                {
                    ISensor? _vramLoad = FindSensor(_hardware!.Sensors, s => s.SensorType == SensorType.Load && s.Name.Contains("Memory")) ??
                        FindSensor(_hardware!.Sensors, s => s.SensorType == SensorType.Load && s.Index == 1);

                    if (_vramLoad != null)
                    {
                        _sensorList.Add(new OHMMetric(_vramLoad, MetricKey.GPUVRAMLoad, DataType.Percent, null, roundAll));
                    }
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUVoltage))
            {
                ISensor? _voltage = FindSensor(_hardware!.Sensors, s => s.SensorType == SensorType.Voltage && s.Index == 0);

                if (_voltage != null)
                {
                    _sensorList.Add(new OHMMetric(_voltage, MetricKey.GPUVoltage, DataType.Voltage, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUTemp))
            {
                ISensor? _tempSensor = FindSensor(_hardware!.Sensors, s => s.SensorType == SensorType.Temperature && s.Index == 0);

                if (_tempSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_tempSensor, MetricKey.GPUTemp, DataType.Celcius, null, roundAll, tempAlert, (useFahrenheit ? CelciusToFahrenheit.Instance : null)));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUFan))
            {
                ISensor? _fanSensor = _hardware!.Sensors.Where(s => s.SensorType == SensorType.Control).OrderBy(s => s.Index).FirstOrDefault();

                if (_fanSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_fanSensor, MetricKey.GPUFan, DataType.Percent));
                }
            }

            Metrics = _sensorList.ToArray();
        }

        private IHardware? _hardware { get; set; }

        [GeneratedRegex(@"^.*(CPU|Core).*#(\d+)$")]
        private static partial Regex CpuCoreClockRegex();

        private static ISensor? FindSensor(ISensor[] sensors, Func<ISensor, bool> predicate)
        {
            return sensors.FirstOrDefault(predicate);
        }

        private static ISensor? FindSensorWithFallback(ISensor[]? fallback, ISensor[] primary, Func<ISensor, bool> fallbackPredicate, Func<ISensor, bool> primaryPredicate)
        {
            if (fallback != null)
            {
                ISensor? sensor = fallback.FirstOrDefault(fallbackPredicate);

                if (sensor != null)
                {
                    return sensor;
                }
            }

            return primary.FirstOrDefault(primaryPredicate);
        }

        private static ISensor[] FindSensors(ISensor[] sensors, Func<ISensor, bool> predicate)
        {
            return sensors.Where(predicate).ToArray();
        }
    }
}
