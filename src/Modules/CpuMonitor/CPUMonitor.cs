using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LibreHardwareMonitor.Hardware;
using SSS.Core;

namespace SSS.Module.CpuMonitor
{
    public partial class CPUMonitor : OHMMonitorBase
    {
        private CPUMonitor(string id, string name, IHardware hardware, IHardware board, MetricConfig[] metrics, bool showHardwareNames, bool roundAll, bool allCoreClocks, bool useGHz, bool useFahrenheit, int tempAlert) : base(id, name, hardware, showHardwareNames)
        {
            InitCPU(board, metrics, roundAll, allCoreClocks, useGHz, useFahrenheit, tempAlert);
        }

        public static CPUMonitor[] GetInstances(Data data, IHardware board, IHardware[] hardware)
        {
            return CreateInstances(data.Hardware!, hardware,
                (c, hw) => new CPUMonitor(c.ID!, c.Name ?? c.ActualName!, hw, board, data.Metrics!.ToArray(), data.ShowHardwareNames, data.RoundAll, data.AllCoreClocks, data.UseGHz, data.UseFahrenheit, data.TempAlert));
        }

        private void InitCPU(IHardware board, MetricConfig[] metrics, bool roundAll, bool allCoreClocks, bool useGHz, bool useFahrenheit, double tempAlert)
        {
            List<OHMMetric> _sensorList = new List<Core.OHMMetric>();

            if (metrics.IsEnabled(MetricKey.CPUClock))
            {
                var coreClocks = Hardware!.Sensors
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
                    board?.Sensors, Hardware!.Sensors,
                    s => s.SensorType == SensorType.Voltage && s.Name.Contains("CPU"),
                    s => s.SensorType == SensorType.Voltage);

                if (_voltage != null)
                {
                    _sensorList.Add(new OHMMetric(_voltage, MetricKey.CPUVoltage, DataType.Voltage, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.CPUTemp))
            {
                ISensor? _tempSensor = FindSensor(Hardware!.Sensors, s => s.SensorType == SensorType.Temperature && s.Name.Contains("CCDs Max (Tdie)"))
                    ?? FindSensorWithFallback(
                        board?.Sensors, Hardware!.Sensors,
                        s => s.SensorType == SensorType.Temperature && s.Name.Contains("CPU"),
                        s => s.SensorType == SensorType.Temperature && (s.Name == "CPU Package" || s.Name.Contains("Tdie")))
                    ?? FindSensor(Hardware!.Sensors, s => s.SensorType == SensorType.Temperature);

                if (_tempSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_tempSensor, MetricKey.CPUTemp, DataType.Celcius, null, roundAll, tempAlert, (useFahrenheit ? CelciusToFahrenheit.Instance : null)));
                }
            }

            if (metrics.IsEnabled(MetricKey.CPUFan))
            {
                static bool IsFanOrControl(ISensor s) => s.SensorType == SensorType.Fan || s.SensorType == SensorType.Control;

                ISensor? _fanSensor = FindSensorWithFallback(
                    board?.Sensors, Hardware!.Sensors,
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
                ISensor[] _loadSensors = FindSensors(Hardware!.Sensors, s => s.SensorType == SensorType.Load);

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
            metrics.ApplyCustomLabels(Metrics);
        }

        [GeneratedRegex(@"^.*(CPU|Core).*#(\d+)$")]
        private static partial Regex CpuCoreClockRegex();
    }
}
