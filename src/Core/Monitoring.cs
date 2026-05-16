using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.IO;

using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Windows.Media;
using LibreHardwareMonitor.Hardware;
using Newtonsoft.Json;
using System.Threading.Tasks;
using SSS.Styling.IconTheme;
using SVGImage.SVG;

namespace SSS.Core
{
    public class MonitorManager : INotifyPropertyChanged, IDisposable
    {
        private IconThemeData _iconTheme;

        public MonitorManager(MonitorConfig[] config)
        {
            _iconTheme = IconThemeData.Load(Settings.Instance.IconTheme);

            _computer = new Computer()
            {
                IsCpuEnabled = true,
                IsControllerEnabled = true,
                IsGpuEnabled = true,
                IsStorageEnabled = false,
                IsMotherboardEnabled = true,
                IsMemoryEnabled = true,
                IsNetworkEnabled = false
            };
            _computer.Open();
            _board = GetHardware(HardwareType.Motherboard).FirstOrDefault();

            UpdateBoard();


            foreach (var c in config)
            {
                if (c.Type == MonitorType.RAM && c.Hardware != null)
                {
                    c.Hardware = c.Hardware
                        .GroupBy(h => h.ID)
                        .Select(g => g.First())
                        .ToArray();
                }
            }

            MonitorPanels = config.Where(c => c.Enabled).OrderByDescending(c => c.Order).Select(c => NewPanel(c)).ToArray();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (MonitorPanel _panel in MonitorPanels)
                    {
                        _panel.Dispose();
                    }

                    _computer.Close();

                    _monitorPanels = null;
                    _computer = null;
                    _board = null;
                }

                _disposed = true;
            }
        }

        ~MonitorManager()
        {
            Dispose(false);
        }

        public HardwareConfig[] GetHardware(MonitorType type)
        {
            switch (type)
            {
                case MonitorType.CPU:
                case MonitorType.RAM:
                case MonitorType.GPU:
                    return GetHardware(type.GetHardwareTypes()).Select(h => new HardwareConfig() { ID = h.Identifier.ToString(), Name = h.Name, ActualName = h.Name }).ToArray();

                case MonitorType.HD:
                    return DriveMonitor.GetHardware().ToArray();

                case MonitorType.Network:
                    return NetworkMonitor.GetHardware().ToArray();

                default:
                    throw new ArgumentException("Invalid MonitorType.");
            }
        }

        public void Update()
        {
            UpdateBoard();

            foreach (iMonitor _monitor in MonitorPanels.SelectMany(p => p.Monitors))
            {
                _monitor.Update();
            }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private IEnumerable<IHardware> GetHardware(params HardwareType[] types)
        {
            return _computer.Hardware.Where(h => types.Contains(h.HardwareType));
        }

        private MonitorPanel NewPanel(MonitorConfig config)
        {
            switch (config.Type)
            {
                case MonitorType.CPU:
                    return OHMPanel(
                        config.Type,
                        Core.Settings.Instance.GetIconSvgPath("cpu"),
                        config.Hardware,
                        config.Metrics,
                        config.Params,
                        config.Type.GetHardwareTypes()
                        );

                case MonitorType.RAM:
                    return OHMPanel(
                        config.Type,
                        Core.Settings.Instance.GetIconSvgPath("ram"),
                        config.Hardware,
                        config.Metrics,
                        config.Params,
                        config.Type.GetHardwareTypes()
                        );

                case MonitorType.GPU:
                    return OHMPanel(
                        config.Type,
                        Core.Settings.Instance.GetIconSvgPath("gpu"),
                        config.Hardware,
                        config.Metrics,
                        config.Params,
                        config.Type.GetHardwareTypes()
                        );

                case MonitorType.HD:
                    return DrivePanel(
                        config.Type,
                        config.Hardware,
                        config.Metrics,
                        config.Params
                        );

                case MonitorType.Network:
                    return NetworkPanel(
                        config.Type,
                        config.Hardware,
                        config.Metrics,
                        config.Params
                        );

                default:
                    throw new ArgumentException("Invalid MonitorType.");
            }
        }

        private MonitorPanel OHMPanel(MonitorType type, string pathData, HardwareConfig[] hardwareConfig, MetricConfig[] metrics, ConfigParam[] parameters, params HardwareType[] hardwareTypes)
        {
            return new MonitorPanel(
                type.GetDescription(),
                pathData,
                OHMMonitor.GetInstances(hardwareConfig, metrics, parameters, type, _board, GetHardware(hardwareTypes).ToArray())
                );
        }

        private MonitorPanel DrivePanel(MonitorType type, HardwareConfig[] hardwareConfig, MetricConfig[] metrics, ConfigParam[] parameters)
        {
            return new MonitorPanel(
                type.GetDescription(),
                Core.Settings.Instance.GetIconSvgPath("hd"),
                DriveMonitor.GetInstances(hardwareConfig, metrics, parameters)
                );
        }

        private MonitorPanel NetworkPanel(MonitorType type, HardwareConfig[] hardwareConfig, MetricConfig[] metrics, ConfigParam[] parameters)
        {
            return new MonitorPanel(
                type.GetDescription(),
                Core.Settings.Instance.GetIconSvgPath("net"),
                NetworkMonitor.GetInstances(hardwareConfig, metrics, parameters)
                );
        }

        private void UpdateBoard()
        {
            _board.Update();
        }

        private MonitorPanel[] _monitorPanels { get; set; }

        public MonitorPanel[] MonitorPanels
        {
            get
            {
                return _monitorPanels;
            }
            private set
            {
                _monitorPanels = value;

                NotifyPropertyChanged("MonitorPanels");
            }
        }

        private Computer _computer { get; set; }

        private IHardware _board { get; set; }

        private bool _disposed { get; set; } = false;
    }

    public class MonitorPanel : INotifyPropertyChanged, IDisposable
    {
        public MonitorPanel(string title, string iconData, params iMonitor[] monitors)
        {
            SvgContentPath = iconData;
            Title = title;

            Monitors = monitors;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (iMonitor _monitor in Monitors)
                    {
                        _monitor.Dispose();
                    }

                    _monitors = null;
                    _svgContentPath = null;
                }

                _disposed = true;
            }
        }

        ~MonitorPanel()
        {
            Dispose(false);
        }
        public ImageSource SvgImageSource
        {
            get
            {
                if (string.IsNullOrEmpty(_svgContentPath)) return null;

                var render = new SVGImage.SVG.SVGRender();
                Color clr = (Color)ColorConverter.ConvertFromString(Core.Settings.Instance.FontColor);
                render.OverrideColor = clr;
                render.OverrideFillColor = clr;
                DrawingGroup drawing = render.LoadDrawing(_svgContentPath);
                if (drawing == null) return null;
                // var brush = new SolidColorBrush(clr);
                // Styling.IconTheme.IconThemeData.ReplaceColor(drawing, brush);
                return new DrawingImage(drawing);
            }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private String _svgContentPath { get; set; }

        public String SvgContentPath
        {
            get
            {
                return _svgContentPath;
            }
            private set
            {
                _svgContentPath = value;
                NotifyPropertyChanged("SvgContent");
                NotifyPropertyChanged("SvgImageSource");
            }
        }

        private string _title { get; set; }

        public string Title
        {
            get
            {
                return _title;
            }
            private set
            {
                _title = value;

                NotifyPropertyChanged("Title");
            }
        }

        private iMonitor[] _monitors { get; set; }

        public iMonitor[] Monitors
        {
            get
            {
                return _monitors;
            }
            private set
            {
                _monitors = value;

                NotifyPropertyChanged("Monitors");
            }
        }

        private bool _disposed { get; set; } = false;
    }

    public interface iMonitor : INotifyPropertyChanged, IDisposable
    {
        string ID { get; }

        string Name { get; }

        bool ShowName { get; }

        iMetric[] Metrics { get; }

        void Update();
    }

    public class BaseMonitor : iMonitor
    {
        public BaseMonitor(string id, string name, bool showName)
        {
            ID = id;
            Name = name;
            ShowName = showName;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (iMetric _metric in Metrics)
                    {
                        _metric.Dispose();
                    }

                    _metrics = null;
                }

                _disposed = true;
            }
        }

        ~BaseMonitor()
        {
            Dispose(false);
        }

        public virtual void Update()
        {
            foreach (iMetric _metric in Metrics)
            {
                _metric.Update();
            }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private string _id { get; set; }

        public string ID
        {
            get
            {
                return _id;
            }
            protected set
            {
                _id = value;

                NotifyPropertyChanged("ID");
            }
        }

        private string _name { get; set; }

        public string Name
        {
            get
            {
                return _name;
            }
            protected set
            {
                _name = value;

                NotifyPropertyChanged("Name");
            }
        }

        private bool _showName { get; set; }

        public bool ShowName
        {
            get
            {
                return _showName;
            }
            protected set
            {
                _showName = value;

                NotifyPropertyChanged("ShowName");
            }
        }

        private iMetric[] _metrics { get; set; }

        public iMetric[] Metrics
        {
            get
            {
                return _metrics;
            }
            protected set
            {
                _metrics = value;

                NotifyPropertyChanged("Metrics");
            }
        }

        protected bool _disposed { get; set; } = false;
    }

    public partial class OHMMonitor : BaseMonitor
    {
        public OHMMonitor(MonitorType type, string id, string name, IHardware hardware, IHardware board, MetricConfig[] metrics, ConfigParam[] parameters) : base(id, name, parameters.GetValue<bool>(ParamKey.HardwareNames))
        {
            _hardware = hardware;

            UpdateHardware();

            switch (type)
            {
                case MonitorType.CPU:
                    InitCPU(
                        board,
                        metrics,
                        parameters.GetValue<bool>(ParamKey.RoundAll),
                        parameters.GetValue<bool>(ParamKey.AllCoreClocks),
                        parameters.GetValue<bool>(ParamKey.UseGHz),
                        parameters.GetValue<bool>(ParamKey.UseFahrenheit),
                        parameters.GetValue<int>(ParamKey.TempAlert)
                        );
                    break;

                case MonitorType.RAM:
                    InitRAM(
                        board,
                        metrics,
                        parameters.GetValue<bool>(ParamKey.RoundAll)
                        );
                    break;

                case MonitorType.GPU:
                    InitGPU(
                        metrics,
                        parameters.GetValue<bool>(ParamKey.RoundAll),
                        parameters.GetValue<bool>(ParamKey.UseGHz),
                        parameters.GetValue<bool>(ParamKey.UseFahrenheit),
                        parameters.GetValue<int>(ParamKey.TempAlert)
                        );
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

        public static iMonitor[] GetInstances(HardwareConfig[] hardwareConfig, MetricConfig[] metrics, ConfigParam[] parameters, MonitorType type, IHardware board, IHardware[] hardware)
        {
            return (
                from hw in hardware
                join c in hardwareConfig on hw.Identifier.ToString() equals c.ID into merged
                from n in merged.DefaultIfEmpty(new HardwareConfig() { ID = hw.Identifier.ToString(), Name = hw.Name, ActualName = hw.Name }).Select(n => { if (n.ActualName != hw.Name) { n.Name = n.ActualName = hw.Name; } return n; })
                where n.Enabled
                orderby n.Order descending, n.Name ascending
                select new OHMMonitor(type, n.ID, n.Name ?? n.ActualName, hw, board, metrics, parameters)
                ).ToArray();
        }

        public override void Update()
        {
            UpdateHardware();

            base.Update();
        }

        private void UpdateHardware()
        {
            _hardware.Update();
        }

        private void InitCPU(IHardware board, MetricConfig[] metrics, bool roundAll, bool allCoreClocks, bool useGHz, bool useFahrenheit, double tempAlert)
        {
            List<OHMMetric> _sensorList = new List<OHMMetric>();

            if (metrics.IsEnabled(MetricKey.CPUClock))
            {
                var coreClocks = _hardware.Sensors
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
                        ISensor firstClock = coreClocks
                            .Select(s => s.Sensor)
                            .FirstOrDefault();

                        _sensorList.Add(new OHMMetric(firstClock, MetricKey.CPUClock, DataType.MHz, null, !useGHz, 0, (useGHz ? MHzToGHz.Instance : null)));
                    }
                }
            }

            if (metrics.IsEnabled(MetricKey.CPUVoltage))
            {
                ISensor? _voltage = FindSensorWithFallback(
                    board?.Sensors, _hardware.Sensors,
                    s => s.SensorType == SensorType.Voltage && s.Name.Contains("CPU"),
                    s => s.SensorType == SensorType.Voltage);

                if (_voltage != null)
                {
                    _sensorList.Add(new OHMMetric(_voltage, MetricKey.CPUVoltage, DataType.Voltage, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.CPUTemp))
            {
                ISensor? _tempSensor = FindSensor(_hardware.Sensors, s => s.SensorType == SensorType.Temperature && s.Name.Contains("CCDs Max (Tdie)")) // Check for AMD core chiplet dies (CCDs)
                    ?? FindSensorWithFallback(
                        board?.Sensors, _hardware.Sensors,
                        s => s.SensorType == SensorType.Temperature && s.Name.Contains("CPU"),
                        s => s.SensorType == SensorType.Temperature && (s.Name == "CPU Package" || s.Name.Contains("Tdie")))
                    ?? FindSensor(_hardware.Sensors, s => s.SensorType == SensorType.Temperature);

                if (_tempSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_tempSensor, MetricKey.CPUTemp, DataType.Celcius, null, roundAll, tempAlert, (useFahrenheit ? CelciusToFahrenheit.Instance : null)));
                }
            }

            if (metrics.IsEnabled(MetricKey.CPUFan))
            {
                static bool IsFanOrControl(ISensor s) => s.SensorType == SensorType.Fan || s.SensorType == SensorType.Control;

                ISensor? _fanSensor = FindSensorWithFallback(
                    board?.Sensors, _hardware.Sensors,
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
                ISensor[] _loadSensors = FindSensors(_hardware.Sensors, s => s.SensorType == SensorType.Load);

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
                ISensor? _ramClock = FindSensor(_hardware.Sensors, s => s.SensorType == SensorType.Clock);

                if (_ramClock != null)
                {
                    _sensorList.Add(new OHMMetric(_ramClock, MetricKey.RAMClock, DataType.MHz, null, true));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMVoltage))
            {
                ISensor? _voltage = FindSensorWithFallback(
                    board?.Sensors, _hardware.Sensors,
                    s => s.SensorType == SensorType.Voltage && s.Name.Contains("RAM"),
                    s => s.SensorType == SensorType.Voltage);

                if (_voltage != null)
                {
                    _sensorList.Add(new OHMMetric(_voltage, MetricKey.RAMVoltage, DataType.Voltage, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMLoad))
            {
                ISensor? _loadSensor = FindSensor(_hardware.Sensors, s => s.SensorType == SensorType.Load && s.Index == 0);

                if (_loadSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_loadSensor, MetricKey.RAMLoad, DataType.Percent, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMUsed))
            {
                ISensor? _usedSensor = FindSensor(_hardware.Sensors, s => s.SensorType == SensorType.Data && s.Index == 0);

                if (_usedSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_usedSensor, MetricKey.RAMUsed, DataType.Gigabyte, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMFree))
            {
                ISensor? _freeSensor = FindSensor(_hardware.Sensors, s => s.SensorType == SensorType.Data && s.Index == 1);

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
                ISensor? _coreClock = FindSensor(_hardware.Sensors, s => s.SensorType == SensorType.Clock && s.Name.Contains("Core"));

                if (_coreClock != null)
                {
                    _sensorList.Add(new OHMMetric(_coreClock, MetricKey.GPUCoreClock, DataType.MHz, null, !useGHz, 0, (useGHz ? MHzToGHz.Instance : null)));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUVRAMClock))
            {
                ISensor? _vramClock = FindSensor(_hardware.Sensors, s => s.SensorType == SensorType.Clock && s.Name.Contains("Memory"));

                if (_vramClock != null)
                {
                    _sensorList.Add(new OHMMetric(_vramClock, MetricKey.GPUVRAMClock, DataType.MHz, null, !useGHz, 0, (useGHz ? MHzToGHz.Instance : null)));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUCoreLoad))
            {
                ISensor? _coreLoad = FindSensor(_hardware.Sensors, s => s.SensorType == SensorType.Load && s.Name.Contains("Core")) ??
                    FindSensor(_hardware.Sensors, s => s.SensorType == SensorType.Load && s.Index == 0);

                if (_coreLoad != null)
                {
                    _sensorList.Add(new OHMMetric(_coreLoad, MetricKey.GPUCoreLoad, DataType.Percent, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUVRAMLoad))
            {
                static bool IsMemoryData(ISensor s) => (s.SensorType == SensorType.Data || s.SensorType == SensorType.SmallData);

                ISensor? _memoryUsed = FindSensor(_hardware.Sensors, s => IsMemoryData(s) && s.Name == "GPU Memory Used");
                ISensor? _memoryTotal = FindSensor(_hardware.Sensors, s => IsMemoryData(s) && s.Name == "GPU Memory Total");

                if (_memoryUsed != null && _memoryTotal != null)
                {
                    _sensorList.Add(new GPUVRAMMLoadMetric(_memoryUsed, _memoryTotal, MetricKey.GPUVRAMLoad, DataType.Percent, null, roundAll));
                }
                else
                {
                    ISensor? _vramLoad = FindSensor(_hardware.Sensors, s => s.SensorType == SensorType.Load && s.Name.Contains("Memory")) ??
                        FindSensor(_hardware.Sensors, s => s.SensorType == SensorType.Load && s.Index == 1);

                    if (_vramLoad != null)
                    {
                        _sensorList.Add(new OHMMetric(_vramLoad, MetricKey.GPUVRAMLoad, DataType.Percent, null, roundAll));
                    }
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUVoltage))
            {
                ISensor? _voltage = FindSensor(_hardware.Sensors, s => s.SensorType == SensorType.Voltage && s.Index == 0);

                if (_voltage != null)
                {
                    _sensorList.Add(new OHMMetric(_voltage, MetricKey.GPUVoltage, DataType.Voltage, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUTemp))
            {
                ISensor? _tempSensor = FindSensor(_hardware.Sensors, s => s.SensorType == SensorType.Temperature && s.Index == 0);

                if (_tempSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_tempSensor, MetricKey.GPUTemp, DataType.Celcius, null, roundAll, tempAlert, (useFahrenheit ? CelciusToFahrenheit.Instance : null)));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUFan))
            {
                ISensor? _fanSensor = _hardware.Sensors.Where(s => s.SensorType == SensorType.Control).OrderBy(s => s.Index).FirstOrDefault();

                if (_fanSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_fanSensor, MetricKey.GPUFan, DataType.Percent));
                }
            }

            Metrics = _sensorList.ToArray();
        }

        private IHardware _hardware { get; set; }

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


    public interface iMetric : INotifyPropertyChanged, IDisposable
    {
        MetricKey Key { get; }

        string FullName { get; }

        string Label { get; }

        double Value { get; }

        string Append { get; }

        double nValue { get; }

        string nAppend { get; }

        string Text { get; }

        bool IsAlert { get; }

        bool IsNumeric { get; }

        void Update();

        void Update(double value);
    }

    public class BaseMetric : iMetric
    {
        public BaseMetric(MetricKey key, DataType dataType, string label = null, bool round = false, double alertValue = 0, iConverter converter = null)
        {
            _converter = converter;
            _round = round;
            _alertValue = alertValue;

            Key = key;

            if (label == null)
            {
                FullName = key.GetFullName();
                Label = key.GetLabel();
            }
            else
            {
                FullName = Label = label;
            }

            nAppend = Append = converter == null ? dataType.GetAppend() : converter.TargetType.GetAppend();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_alertColorTimer != null)
                    {
                        _alertColorTimer.Stop();
                        _alertColorTimer = null;
                    }

                    _converter = null;
                }

                _disposed = true;
            }
        }

        ~BaseMetric()
        {
            Dispose(false);
        }

        public virtual void Update() { }

        public void Update(double value)
        {
            double _val = value;

            if (_converter == null)
            {
                nValue = _val;
            }
            else if (_converter.IsDynamic)
            {
                double _nVal;
                DataType _dataType;

                _converter.Convert(ref _val, out _nVal, out _dataType);

                nValue = _nVal;
                Append = _dataType.GetAppend();
            }
            else
            {
                _converter.Convert(ref _val);

                nValue = _val;
            }

            Value = _val;

            if (_alertValue > 0 && _alertValue <= nValue)
            {
                if (!IsAlert)
                {
                    IsAlert = true;
                }
            }
            else if (IsAlert)
            {
                IsAlert = false;
            }

            Text = string.Format(
                "{0:#,##0.##}{1}",
                _val.Round(_round),
                Append
                );
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private MetricKey _key { get; set; }

        public MetricKey Key
        {
            get
            {
                return _key;
            }
            protected set
            {
                _key = value;

                NotifyPropertyChanged("Key");
            }
        }

        private string _fullName { get; set; }

        public string FullName
        {
            get
            {
                return _fullName;
            }
            protected set
            {
                _fullName = value;

                NotifyPropertyChanged("FullName");
            }
        }

        private string _label { get; set; }

        public string Label
        {
            get
            {
                return _label;
            }
            protected set
            {
                _label = value;

                NotifyPropertyChanged("Label");
            }
        }

        private double _value { get; set; }

        public double Value
        {
            get
            {
                return _value;
            }
            protected set
            {
                _value = value;

                NotifyPropertyChanged("Value");
            }
        }

        private string _append { get; set; }

        public string Append
        {
            get
            {
                return _append;
            }
            protected set
            {
                _append = value;

                NotifyPropertyChanged("Append");
            }
        }

        private double _nValue { get; set; }

        public double nValue
        {
            get
            {
                return _nValue;
            }
            set
            {
                _nValue = value;

                NotifyPropertyChanged("nValue");
            }
        }

        private string _nAppend { get; set; }

        public string nAppend
        {
            get
            {
                return _nAppend;
            }
            set
            {
                _nAppend = value;

                NotifyPropertyChanged("nAppend");
            }
        }

        private string _text { get; set; }

        public string Text
        {
            get
            {
                return _text;
            }
            protected set
            {
                _text = value;

                NotifyPropertyChanged("Text");
            }
        }

        private bool _isAlert { get; set; }

        public bool IsAlert
        {
            get
            {
                return _isAlert;
            }
            protected set
            {
                _isAlert = value;

                NotifyPropertyChanged("IsAlert");

                if (value)
                {
                    _alertColorFlag = false;

                    if (Core.Settings.Instance.AlertBlink)
                    {
                        _alertColorTimer = new DispatcherTimer(DispatcherPriority.Normal, App.Current.Dispatcher);
                        _alertColorTimer.Interval = TimeSpan.FromSeconds(0.5d);
                        _alertColorTimer.Tick += new EventHandler(AlertColorTimer_Tick);
                        _alertColorTimer.Start();
                    }
                }
                else if (_alertColorTimer != null)
                {
                    _alertColorTimer.Stop();
                    _alertColorTimer = null;
                }
            }
        }

        public virtual bool IsNumeric
        {
            get { return true; }
        }

        public string AlertColor
        {
            get
            {
                return _alertColorFlag ? Settings.Instance.FontColor : Settings.Instance.AlertFontColor;
            }
        }

        private DispatcherTimer _alertColorTimer;

        private void AlertColorTimer_Tick(object sender, EventArgs e)
        {
            _alertColorFlag = !_alertColorFlag;

            NotifyPropertyChanged("AlertColor");
        }

        private bool _alertColorFlag = false;

        protected iConverter _converter { get; set; }

        protected bool _round { get; set; }

        protected double _alertValue { get; set; }

        protected bool _disposed { get; set; } = false;
    }

    public class OHMMetric : BaseMetric
    {
        public OHMMetric(ISensor sensor, MetricKey key, DataType dataType, string label = null, bool round = false, double alertValue = 0, iConverter converter = null) : base(key, dataType, label, round, alertValue, converter)
        {
            _sensor = sensor;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _sensor = null;
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }

        ~OHMMetric()
        {
            Dispose(false);
        }

        public override void Update()
        {
            if (_sensor.Value.HasValue)
            {
                Update(_sensor.Value.Value);
            }
            else
            {
                Text = "No Value";
            }
        }

        private ISensor _sensor { get; set; }
    }

    public class GPUVRAMMLoadMetric : BaseMetric
    {
        public GPUVRAMMLoadMetric(ISensor memoryUsedSensor, ISensor memoryTotalSensor, MetricKey key, DataType dataType, string label = null, bool round = false, double alertValue = 0, iConverter converter = null) : base(key, dataType, label, round, alertValue, converter)
        {
            _memoryUsedSensor = memoryUsedSensor;
            _memoryTotalSensor = memoryTotalSensor;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _memoryUsedSensor = null;
                    _memoryTotalSensor = null;
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }

        ~GPUVRAMMLoadMetric()
        {
            Dispose(false);
        }

        public override void Update()
        {
            if (_memoryUsedSensor.Value.HasValue && _memoryTotalSensor.Value.HasValue)
            {
                float load = _memoryUsedSensor.Value.Value / _memoryTotalSensor.Value.Value * 100f;

                Update(load);
            }
            else
            {
                Text = "No Value";
            }
        }

        private ISensor _memoryUsedSensor { get; set; }

        private ISensor _memoryTotalSensor { get; set; }
    }

    public class IPMetric : BaseMetric
    {
        public IPMetric(string ipAddress, MetricKey key, DataType dataType, string label = null, bool round = false, double alertValue = 0, iConverter converter = null) : base(key, dataType, label, round, alertValue, converter)
        {
            Text = ipAddress;
        }

        ~IPMetric()
        {
            Dispose(false);
        }

        public override bool IsNumeric
        {
            get { return false; }
        }
    }

    public class PCMetric : BaseMetric
    {
        public PCMetric(PerformanceCounter counter, MetricKey key, DataType dataType, string label = null, bool round = false, double alertValue = 0, iConverter converter = null) : base(key, dataType, label, round, alertValue, converter)
        {
            _counter = counter;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_counter != null)
                    {
                        _counter.Dispose();
                        _counter = null;
                    }
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }

        ~PCMetric()
        {
            Dispose(false);
        }

        public override void Update()
        {
            Update(_counter.NextValue());
        }

        private PerformanceCounter _counter { get; set; }
    }

    [Serializable]
    public enum MonitorType : byte
    {
        CPU,
        RAM,
        GPU,
        HD,
        Network
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MonitorConfig : INotifyPropertyChanged, ICloneable
    {
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MonitorConfig Clone()
        {
            MonitorConfig _clone = (MonitorConfig)MemberwiseClone();
            _clone.Hardware = _clone.Hardware.Select(h => h.Clone()).ToArray();
            _clone.Params = _clone.Params.Select(p => p.Clone()).ToArray();

            if (_clone.HardwareOC != null)
            {
                _clone.HardwareOC = new ObservableCollection<HardwareConfig>(_clone.HardwareOC.Select(h => h.Clone()));
            }

            return _clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private MonitorType _type { get; set; }

        [JsonProperty]
        public MonitorType Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;

                NotifyPropertyChanged("Type");
            }
        }

        private bool _enabled { get; set; }

        [JsonProperty]
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;

                NotifyPropertyChanged("Enabled");
            }
        }

        private byte _order { get; set; }

        [JsonProperty]
        public byte Order
        {
            get
            {
                return _order;
            }
            set
            {
                _order = value;

                NotifyPropertyChanged("Order");
            }
        }

        private HardwareConfig[] _hardware { get; set; }

        [JsonProperty]
        public HardwareConfig[] Hardware
        {
            get
            {
                return _hardware;
            }
            set
            {
                _hardware = value;

                NotifyPropertyChanged("Hardware");
            }
        }

        private ObservableCollection<HardwareConfig> _hardwareOC { get; set; }

        public ObservableCollection<HardwareConfig> HardwareOC
        {
            get
            {
                return _hardwareOC;
            }
            set
            {
                _hardwareOC = value;

                NotifyPropertyChanged("HardwareOC");
            }
        }

        private MetricConfig[] _metrics { get; set; }

        [JsonProperty]
        public MetricConfig[] Metrics
        {
            get
            {
                return _metrics;
            }
            set
            {
                _metrics = value;

                NotifyPropertyChanged("Metrics");
            }
        }

        private ConfigParam[] _params { get; set; }

        [JsonProperty]
        public ConfigParam[] Params
        {
            get
            {
                return _params;
            }
            set
            {
                _params = value;

                NotifyPropertyChanged("Params");
            }
        }

        public string Name
        {
            get
            {
                return Type.GetDescription();
            }
        }

        public static MonitorConfig[] CheckConfig(MonitorConfig[] config)
        {
            MonitorConfig[] _default = Default;

            if (config == null)
            {
                return _default;
            }

            config = (
                from def in _default
                join rec in config on def.Type equals rec.Type into merged
                from newrec in merged.DefaultIfEmpty(def)
                select newrec
                ).ToArray();

            foreach (MonitorConfig _record in config)
            {
                MonitorConfig _defaultRecord = _default.Single(d => d.Type == _record.Type);

                if (_record.Hardware == null)
                {
                    _record.Hardware = _defaultRecord.Hardware;
                }

                if (_record.Metrics == null)
                {
                    _record.Metrics = _defaultRecord.Metrics;
                }
                else
                {
                    _record.Metrics = (
                        from def in _defaultRecord.Metrics
                        join metric in _record.Metrics on def.Key equals metric.Key into merged
                        from newmetric in merged.DefaultIfEmpty(def)
                        select newmetric
                        ).ToArray();
                }

                if (_record.Params == null)
                {
                    _record.Params = _defaultRecord.Params;
                }
                else
                {
                    _record.Params = (
                        from def in _defaultRecord.Params
                        join param in _record.Params on def.Key equals param.Key into merged
                        from newparam in merged.DefaultIfEmpty(def)
                        select newparam
                        ).ToArray();
                }
            }

            return config;
        }

        public static MonitorConfig[] Default
        {
            get
            {
                return new MonitorConfig[5]
                {
                    new MonitorConfig()
                    {
                        Type = MonitorType.CPU,
                        Enabled = true,
                        Order = 5,
                        Hardware = new HardwareConfig[0],
                        Metrics = new MetricConfig[6]
                        {
                            new MetricConfig(MetricKey.CPUClock, true),
                            new MetricConfig(MetricKey.CPUTemp, true),
                            new MetricConfig(MetricKey.CPUVoltage, true),
                            new MetricConfig(MetricKey.CPUFan, true),
                            new MetricConfig(MetricKey.CPULoad, true),
                            new MetricConfig(MetricKey.CPUCoreLoad, true)
                        },
                        Params = new ConfigParam[6]
                        {
                            ConfigParam.Defaults.HardwareNames,
                            ConfigParam.Defaults.RoundAll,
                            ConfigParam.Defaults.AllCoreClocks,
                            ConfigParam.Defaults.UseGHz,
                            ConfigParam.Defaults.UseFahrenheit,
                            ConfigParam.Defaults.TempAlert
                        }
                    },
                    new MonitorConfig()
                    {
                        Type = MonitorType.RAM,
                        Enabled = true,
                        Order = 4,
                        Hardware = new HardwareConfig[0],
                        Metrics = new MetricConfig[5]
                        {
                            new MetricConfig(MetricKey.RAMClock, true),
                            new MetricConfig(MetricKey.RAMVoltage, true),
                            new MetricConfig(MetricKey.RAMLoad, true),
                            new MetricConfig(MetricKey.RAMUsed, true),
                            new MetricConfig(MetricKey.RAMFree, true)
                        },
                        Params = new ConfigParam[2]
                        {
                            ConfigParam.Defaults.NoHardwareNames,
                            ConfigParam.Defaults.RoundAll
                        }
                    },
                    new MonitorConfig()
                    {
                        Type = MonitorType.GPU,
                        Enabled = true,
                        Order = 3,
                        Hardware = new HardwareConfig[0],
                        Metrics = new MetricConfig[7]
                        {
                            new MetricConfig(MetricKey.GPUCoreClock, true),
                            new MetricConfig(MetricKey.GPUVRAMClock, true),
                            new MetricConfig(MetricKey.GPUCoreLoad, true),
                            new MetricConfig(MetricKey.GPUVRAMLoad, true),
                            new MetricConfig(MetricKey.GPUVoltage, true),
                            new MetricConfig(MetricKey.GPUTemp, true),
                            new MetricConfig(MetricKey.GPUFan, true)
                        },
                        Params = new ConfigParam[5]
                        {
                            ConfigParam.Defaults.HardwareNames,
                            ConfigParam.Defaults.RoundAll,
                            ConfigParam.Defaults.UseGHz,
                            ConfigParam.Defaults.UseFahrenheit,
                            ConfigParam.Defaults.TempAlert
                        }
                    },
                    new MonitorConfig()
                    {
                        Type = MonitorType.HD,
                        Enabled = true,
                        Order = 2,
                        Hardware = new HardwareConfig[0],
                        Metrics = new MetricConfig[6]
                        {
                            new MetricConfig(MetricKey.DriveLoadBar, true),
                            new MetricConfig(MetricKey.DriveLoad, true),
                            new MetricConfig(MetricKey.DriveUsed, true),
                            new MetricConfig(MetricKey.DriveFree, true),
                            new MetricConfig(MetricKey.DriveRead, true),
                            new MetricConfig(MetricKey.DriveWrite, true)
                        },
                        Params = new ConfigParam[2]
                        {
                            ConfigParam.Defaults.RoundAll,
                            ConfigParam.Defaults.UsedSpaceAlert
                        }
                    },
                    new MonitorConfig()
                    {
                        Type = MonitorType.Network,
                        Enabled = true,
                        Order = 1,
                        Hardware = new HardwareConfig[0],
                        Metrics = new MetricConfig[4]
                        {
                            new MetricConfig(MetricKey.NetworkIP, true),
                            new MetricConfig(MetricKey.NetworkExtIP, false),
                            new MetricConfig(MetricKey.NetworkIn, true),
                            new MetricConfig(MetricKey.NetworkOut, true)
                        },
                        Params = new ConfigParam[5]
                        {
                            ConfigParam.Defaults.HardwareNames,
                            ConfigParam.Defaults.RoundAll,
                            ConfigParam.Defaults.UseBytes,
                            ConfigParam.Defaults.BandwidthInAlert,
                            ConfigParam.Defaults.BandwidthOutAlert
                        }
                    }
                };
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class HardwareConfig : INotifyPropertyChanged, ICloneable
    {
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public HardwareConfig Clone()
        {
            return (HardwareConfig)MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private string _id { get; set; }

        [JsonProperty]
        public string ID
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;

                NotifyPropertyChanged("ID");
            }
        }

        private string _name { get; set; }

        [JsonProperty]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;

                NotifyPropertyChanged("Name");
            }
        }

        private string _actualName { get; set; }

        [JsonProperty]
        public string ActualName
        {
            get
            {
                return _actualName;
            }
            set
            {
                _actualName = value;

                NotifyPropertyChanged("ActualName");
            }
        }

        private bool _enabled { get; set; } = true;

        [JsonProperty]
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;

                NotifyPropertyChanged("Enabled");
            }
        }

        private byte _order { get; set; } = 0;

        [JsonProperty]
        public byte Order
        {
            get
            {
                return _order;
            }
            set
            {
                _order = value;

                NotifyPropertyChanged("Order");
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MetricConfig : INotifyPropertyChanged, ICloneable
    {
        public MetricConfig() { }

        public MetricConfig(MetricKey key, bool enabled)
        {
            Key = key;
            Enabled = enabled;
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ConfigParam Clone()
        {
            return (ConfigParam)MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private MetricKey _key { get; set; }

        [JsonProperty]
        public MetricKey Key
        {
            get
            {
                return _key;
            }
            set
            {
                _key = value;

                NotifyPropertyChanged("Key");
            }
        }

        private bool _enabled { get; set; }

        [JsonProperty]
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if (_enabled == value)
                {
                    return;
                }

                _enabled = value;

                NotifyPropertyChanged("Enabled");
            }
        }

        public string Name
        {
            get
            {
                return Key.GetFullName();
            }
        }
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
        DriveWrite = 25
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ConfigParam : INotifyPropertyChanged, ICloneable
    {
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ConfigParam Clone()
        {
            return (ConfigParam)MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private ParamKey _key { get; set; }

        [JsonProperty]
        public ParamKey Key
        {
            get
            {
                return _key;
            }
            set
            {
                _key = value;

                NotifyPropertyChanged("Key");
            }
        }

        private object _value { get; set; }

        [JsonProperty]
        public object Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (value.GetType() == typeof(long))
                {
                    _value = Convert.ToInt32(value);
                }
                else
                {
                    _value = value;
                }

                NotifyPropertyChanged("Value");
            }
        }

        public Type Type
        {
            get
            {
                return Value.GetType();
            }
        }

        public string TypeString
        {
            get
            {
                return Type.ToString();
            }
        }

        public string Name
        {
            get
            {
                switch (Key)
                {
                    case ParamKey.HardwareNames:
                        return Strings.SettingsShowHardwareNames;

                    case ParamKey.UseFahrenheit:
                        return Strings.SettingsUseFahrenheit;

                    case ParamKey.AllCoreClocks:
                        return Strings.SettingsAllCoreClocks;

                    case ParamKey.CoreLoads:
                        return Strings.SettingsCoreLoads;

                    case ParamKey.TempAlert:
                        return Strings.SettingsTemperatureAlert;

                    case ParamKey.DriveDetails:
                        return Strings.SettingsShowDriveDetails;

                    case ParamKey.UsedSpaceAlert:
                        return Strings.SettingsUsedSpaceAlert;

                    case ParamKey.BandwidthInAlert:
                        return Strings.SettingsBandwidthInAlert;

                    case ParamKey.BandwidthOutAlert:
                        return Strings.SettingsBandwidthOutAlert;

                    case ParamKey.UseBytes:
                        return Strings.SettingsUseBytesPerSecond;

                    case ParamKey.RoundAll:
                        return Strings.SettingsRoundAllDecimals;

                    case ParamKey.DriveSpace:
                        return Strings.SettingsShowDriveSpace;

                    case ParamKey.DriveIO:
                        return Strings.SettingsShowDriveIO;

                    case ParamKey.UseGHz:
                        return Strings.SettingsUseGHz;

                    default:
                        return "Unknown";
                }
            }
        }

        public string Tooltip
        {
            get
            {
                switch (Key)
                {
                    case ParamKey.HardwareNames:
                        return Strings.SettingsShowHardwareNamesTooltip;

                    case ParamKey.UseFahrenheit:
                        return Strings.SettingsUseFahrenheitTooltip;

                    case ParamKey.AllCoreClocks:
                        return Strings.SettingsAllCoreClocksTooltip;

                    case ParamKey.CoreLoads:
                        return Strings.SettingsCoreLoadsTooltip;

                    case ParamKey.TempAlert:
                        return Strings.SettingsTemperatureAlertTooltip;

                    case ParamKey.DriveDetails:
                        return Strings.SettingsDriveDetailsTooltip;

                    case ParamKey.UsedSpaceAlert:
                        return Strings.SettingsUsedSpaceAlertTooltip;

                    case ParamKey.BandwidthInAlert:
                        return Strings.SettingsBandwidthInAlertTooltip;

                    case ParamKey.BandwidthOutAlert:
                        return Strings.SettingsBandwidthOutAlertTooltip;

                    case ParamKey.UseBytes:
                        return Strings.SettingsUseBytesPerSecondTooltip;

                    case ParamKey.RoundAll:
                        return Strings.SettingsRoundAllDecimalsTooltip;

                    case ParamKey.DriveSpace:
                        return Strings.SettingsShowDriveSpaceTooltip;

                    case ParamKey.DriveIO:
                        return Strings.SettingsShowDriveIOTooltip;

                    case ParamKey.UseGHz:
                        return Strings.SettingsUseGHzTooltip;

                    default:
                        return "Unknown";
                }
            }
        }

        public static class Defaults
        {
            public static ConfigParam HardwareNames
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.HardwareNames, Value = true };
                }
            }

            public static ConfigParam NoHardwareNames
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.HardwareNames, Value = false };
                }
            }

            public static ConfigParam UseFahrenheit
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.UseFahrenheit, Value = false };
                }
            }

            public static ConfigParam AllCoreClocks
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.AllCoreClocks, Value = false };
                }
            }

            public static ConfigParam CoreLoads
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.CoreLoads, Value = true };
                }
            }

            public static ConfigParam TempAlert
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.TempAlert, Value = 0 };
                }
            }

            public static ConfigParam DriveDetails
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.DriveDetails, Value = false };
                }
            }

            public static ConfigParam UsedSpaceAlert
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.UsedSpaceAlert, Value = 0 };
                }
            }

            public static ConfigParam BandwidthInAlert
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.BandwidthInAlert, Value = 0 };
                }
            }

            public static ConfigParam BandwidthOutAlert
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.BandwidthOutAlert, Value = 0 };
                }
            }

            public static ConfigParam UseBytes
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.UseBytes, Value = false };
                }
            }

            public static ConfigParam RoundAll
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.RoundAll, Value = false };
                }
            }

            public static ConfigParam ShowDriveSpace
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.DriveSpace, Value = true };
                }
            }

            public static ConfigParam ShowDriveIO
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.DriveIO, Value = true };
                }
            }

            public static ConfigParam UseGHz
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.UseGHz, Value = false };
                }
            }
        }
    }

    [Serializable]
    public enum ParamKey : byte
    {
        HardwareNames,
        UseFahrenheit,
        AllCoreClocks,
        CoreLoads,
        TempAlert,
        DriveDetails,
        UsedSpaceAlert,
        BandwidthInAlert,
        BandwidthOutAlert,
        UseBytes,
        RoundAll,
        DriveSpace,
        DriveIO,
        UseGHz
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

    public interface iConverter
    {
        void Convert(ref double value);

        void Convert(ref double value, out double normalized, out DataType targetType);

        DataType TargetType { get; }

        bool IsDynamic { get; }
    }

    public class CelciusToFahrenheit : iConverter
    {
        private CelciusToFahrenheit() { }

        public void Convert(ref double value)
        {
            value = value * 1.8d + 32d;
        }

        public void Convert(ref double value, out double normalized, out DataType targetType)
        {
            Convert(ref value);
            normalized = value;
            targetType = TargetType;
        }

        public DataType TargetType
        {
            get
            {
                return DataType.Fahrenheit;
            }
        }

        public bool IsDynamic
        {
            get
            {
                return false;
            }
        }

        private static CelciusToFahrenheit _instance { get; set; } = null;

        public static CelciusToFahrenheit Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CelciusToFahrenheit();
                }

                return _instance;
            }
        }
    }

    public class MHzToGHz : iConverter
    {
        private MHzToGHz() { }

        public void Convert(ref double value)
        {
            value = value / 1000d;
        }

        public void Convert(ref double value, out double normalized, out DataType targetType)
        {
            Convert(ref value);
            normalized = value;
            targetType = TargetType;
        }

        public DataType TargetType
        {
            get
            {
                return DataType.GHz;
            }
        }

        public bool IsDynamic
        {
            get
            {
                return false;
            }
        }

        private static MHzToGHz _instance { get; set; } = null;

        public static MHzToGHz Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MHzToGHz();
                }

                return _instance;
            }
        }
    }

    public class BitsPerSecondConverter : iConverter
    {
        private BitsPerSecondConverter() { }

        public void Convert(ref double value)
        {
            double _normalized;
            DataType _dataType;

            Convert(ref value, out _normalized, out _dataType);
        }

        public void Convert(ref double value, out double normalized, out DataType targetType)
        {
            normalized = value /= 128d;

            if (value < 1024d)
            {
                targetType = DataType.kbps;
                return;
            }
            else if (value < 1048576d)
            {
                value /= 1024d;
                targetType = DataType.Mbps;
                return;
            }
            else
            {
                value /= 1048576d;
                targetType = DataType.Gbps;
                return;
            }
        }

        public DataType TargetType
        {
            get
            {
                return DataType.kbps;
            }
        }

        public bool IsDynamic
        {
            get
            {
                return true;
            }
        }

        private static BitsPerSecondConverter _instance { get; set; } = null;

        public static BitsPerSecondConverter Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BitsPerSecondConverter();
                }

                return _instance;
            }
        }
    }

    public class BytesPerSecondConverter : iConverter
    {
        private BytesPerSecondConverter() { }

        public void Convert(ref double value)
        {
            double _normalized;
            DataType _dataType;

            Convert(ref value, out _normalized, out _dataType);
        }

        public void Convert(ref double value, out double normalized, out DataType targetType)
        {
            normalized = value /= 1024d;

            if (value < 1024d)
            {
                targetType = DataType.kBps;
                return;
            }
            else if (value < 1048576d)
            {
                value /= 1024d;
                targetType = DataType.MBps;
                return;
            }
            else
            {
                value /= 1048576d;
                targetType = DataType.GBps;
                return;
            }
        }

        public DataType TargetType
        {
            get
            {
                return DataType.kBps;
            }
        }

        public bool IsDynamic
        {
            get
            {
                return true;
            }
        }

        private static BytesPerSecondConverter _instance { get; set; } = null;

        public static BytesPerSecondConverter Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BytesPerSecondConverter();
                }

                return _instance;
            }
        }
    }

    public static class Extensions
    {
        public static bool IsEnabled(this MetricConfig[] metrics, MetricKey key)
        {
            return metrics.Any(m => m.Key == key && m.Enabled);
        }

        public static HardwareType[] GetHardwareTypes(this MonitorType type)
        {
            switch (type)
            {
                case MonitorType.CPU:
                    return new HardwareType[1] { HardwareType.Cpu };

                case MonitorType.RAM:
                    return new HardwareType[1] { HardwareType.Memory };

                case MonitorType.GPU:
                    return new HardwareType[2] { HardwareType.GpuNvidia, HardwareType.GpuAmd };

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

                default:
                    throw new ArgumentException("Invalid MonitorType.");
            }
        }

        public static T GetValue<T>(this ConfigParam[] parameters, ParamKey key)
        {
            return (T)parameters.Single(p => p.Key == key).Value;
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