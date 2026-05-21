using System;
using System.Collections.Generic;
using System.Linq;
using LibreHardwareMonitor.Hardware;
using SSS.Core;

namespace SSS.Module.GpuMonitor
{
    public partial class GPUMonitor : OHMMonitorBase
    {
        private GPUMonitor(string id, string name, IHardware hardware, MetricConfig[] metrics, bool showHardwareNames, bool roundAll, bool useGHz, bool useFahrenheit, int tempAlert) : base(id, name, hardware, showHardwareNames)
        {
            InitGPU(metrics, roundAll, useGHz, useFahrenheit, tempAlert);
        }

        public static GPUMonitor[] GetInstances(Data data, IHardware board, IHardware[] hardware)
        {
            return CreateInstances(data.Hardware!, hardware,
                (c, hw) => new GPUMonitor(c.ID!, c.Name ?? c.ActualName!, hw, data.Metrics!.ToArray(), data.ShowHardwareNames, data.RoundAll, data.UseGHz, data.UseFahrenheit, data.TempAlert));
        }

        private void InitGPU(MetricConfig[] metrics, bool roundAll, bool useGHz, bool useFahrenheit, double tempAlert)
        {
            List<iMetric> _sensorList = new List<Core.iMetric>();

            if (metrics.IsEnabled(MetricKey.GPUCoreClock))
            {
                ISensor? _coreClock = FindSensor(Hardware!.Sensors, s => s.SensorType == SensorType.Clock && s.Name.Contains("Core"));

                if (_coreClock != null)
                {
                    _sensorList.Add(new OHMMetric(_coreClock, MetricKey.GPUCoreClock, DataType.MHz, null, !useGHz, 0, (useGHz ? MHzToGHz.Instance : null)));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUVRAMClock))
            {
                ISensor? _vramClock = FindSensor(Hardware!.Sensors, s => s.SensorType == SensorType.Clock && s.Name.Contains("Memory"));

                if (_vramClock != null)
                {
                    _sensorList.Add(new OHMMetric(_vramClock, MetricKey.GPUVRAMClock, DataType.MHz, null, !useGHz, 0, (useGHz ? MHzToGHz.Instance : null)));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUCoreLoad))
            {
                ISensor? _coreLoad = FindSensor(Hardware!.Sensors, s => s.SensorType == SensorType.Load && s.Name.Contains("Core")) ??
                    FindSensor(Hardware!.Sensors, s => s.SensorType == SensorType.Load && s.Index == 0);

                if (_coreLoad != null)
                {
                    _sensorList.Add(new OHMMetric(_coreLoad, MetricKey.GPUCoreLoad, DataType.Percent, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUVRAMLoad))
            {
                static bool IsMemoryData(ISensor s) => (s.SensorType == SensorType.Data || s.SensorType == SensorType.SmallData);

                ISensor? _memoryUsed = FindSensor(Hardware!.Sensors, s => IsMemoryData(s) && s.Name == "GPU Memory Used");
                ISensor? _memoryTotal = FindSensor(Hardware!.Sensors, s => IsMemoryData(s) && s.Name == "GPU Memory Total");

                if (_memoryUsed != null && _memoryTotal != null)
                {
                    _sensorList.Add(new GPUVRAMMLoadMetric(_memoryUsed, _memoryTotal, MetricKey.GPUVRAMLoad, DataType.Percent, null, roundAll));
                }
                else
                {
                    ISensor? _vramLoad = FindSensor(Hardware!.Sensors, s => s.SensorType == SensorType.Load && s.Name.Contains("Memory")) ??
                        FindSensor(Hardware!.Sensors, s => s.SensorType == SensorType.Load && s.Index == 1);

                    if (_vramLoad != null)
                    {
                        _sensorList.Add(new OHMMetric(_vramLoad, MetricKey.GPUVRAMLoad, DataType.Percent, null, roundAll));
                    }
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUVoltage))
            {
                ISensor? _voltage = FindSensor(Hardware!.Sensors, s => s.SensorType == SensorType.Voltage && s.Index == 0);

                if (_voltage != null)
                {
                    _sensorList.Add(new OHMMetric(_voltage, MetricKey.GPUVoltage, DataType.Voltage, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUTemp))
            {
                ISensor? _tempSensor = FindSensor(Hardware!.Sensors, s => s.SensorType == SensorType.Temperature && s.Index == 0);

                if (_tempSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_tempSensor, MetricKey.GPUTemp, DataType.Celcius, null, roundAll, tempAlert, (useFahrenheit ? CelciusToFahrenheit.Instance : null)));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUFan))
            {
                ISensor? _fanSensor = Hardware!.Sensors.Where(s => s.SensorType == SensorType.Control).OrderBy(s => s.Index).FirstOrDefault();

                if (_fanSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_fanSensor, MetricKey.GPUFan, DataType.Percent));
                }
            }

            Metrics = _sensorList.ToArray();
            metrics.ApplyCustomLabels(Metrics);
        }
    }
}
