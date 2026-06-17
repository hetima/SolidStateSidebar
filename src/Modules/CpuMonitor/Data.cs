using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using SSS.Core;

namespace SSS.Module.CpuMonitor
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Data : ObservableObject, Core.IModuleData, ICloneable
    {
        // --- IModuleData ---

        private bool _enabled = true;

        [JsonProperty("enabled")]
        public bool Enabled
        {
            get => _enabled;
            set => SetProperty(ref _enabled, value);
        }

        private byte _order = 5;

        [JsonProperty("order")]
        public byte Order
        {
            get => _order;
            set => SetProperty(ref _order, value);
        }

        private HardwareConfig[] _hardware = [];

        [JsonProperty("hardware")]
        public HardwareConfig[] Hardware
        {
            get => _hardware;
            set => SetProperty(ref _hardware, value);
        }

        private ObservableCollection<MetricConfig> _metrics = [];

        [JsonProperty("metrics")]
        public ObservableCollection<MetricConfig> Metrics
        {
            get => _metrics;
            set
            {
                if (_metrics != null)
                    foreach (var item in _metrics)
                        item.PropertyChanged -= OnMetricPropertyChanged;
                _metrics = value;
                if (_metrics != null)
                    foreach (var item in _metrics)
                        item.PropertyChanged += OnMetricPropertyChanged;
                NotifyPropertyChanged();
            }
        }

        private void OnMetricPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MetricConfig.Enabled))
                NotifyPropertyChanged(nameof(Metrics));
        }

        // --- UI-only (not serialized) ---

        [JsonIgnore]
        public string Name => Strings.CPU;

        [JsonIgnore]
        public ObservableCollection<HardwareConfig>? HardwareOC { get; set; }

        // --- CpuMonitor-specific params ---

        private bool _showHardwareNames = true;

        [JsonProperty("showHardwareNames")]
        public bool ShowHardwareNames
        {
            get => _showHardwareNames;
            set => SetProperty(ref _showHardwareNames, value);
        }

        private bool _roundAll = false;

        [JsonProperty("roundAll")]
        public bool RoundAll
        {
            get => _roundAll;
            set => SetProperty(ref _roundAll, value);
        }

        private bool _allCoreClocks = false;

        [JsonProperty("allCoreClocks")]
        public bool AllCoreClocks
        {
            get => _allCoreClocks;
            set => SetProperty(ref _allCoreClocks, value);
        }

        private bool _useGHz = false;

        [JsonProperty("useGHz")]
        public bool UseGHz
        {
            get => _useGHz;
            set => SetProperty(ref _useGHz, value);
        }

        private bool _useFahrenheit = false;

        [JsonProperty("useFahrenheit")]
        public bool UseFahrenheit
        {
            get => _useFahrenheit;
            set => SetProperty(ref _useFahrenheit, value);
        }

        private int _tempAlert = 0;

        [JsonProperty("tempAlert")]
        public int TempAlert
        {
            get => _tempAlert;
            set => SetProperty(ref _tempAlert, value);
        }

        private SectionHeaderStyle _sectionHeaderStyle = SectionHeaderStyle.Default;

        [JsonProperty("sectionHeaderStyle")]
        public SectionHeaderStyle SectionHeaderStyle
        {
            get => _sectionHeaderStyle;
            set => SetProperty(ref _sectionHeaderStyle, value);
        }

        // --- Defaults & Clone ---

        public static Data Default => new Data
        {
            Enabled = true,
            Order = 5,
            Hardware = [],
            Metrics = new ObservableCollection<MetricConfig>(
            [
                new MetricConfig(MetricKey.CPUClock, true),
                new MetricConfig(MetricKey.CPUTemp, true),
                new MetricConfig(MetricKey.CPUVoltage, true),
                new MetricConfig(MetricKey.CPUFan, true),
                new MetricConfig(MetricKey.CPULoad, true),
                new MetricConfig(MetricKey.CPUCoreLoad, true)
            ]),
            ShowHardwareNames = true,
            RoundAll = false,
            AllCoreClocks = false,
            UseGHz = false,
            UseFahrenheit = false,
            TempAlert = 0
        };

        public Data Clone()
        {
            Data clone = (Data)MemberwiseClone();
            clone.Hardware = clone.Hardware.Select(h => h.Clone()).ToArray();
            clone.Metrics = new ObservableCollection<MetricConfig>(clone.Metrics.Select(m => m.Clone()));
            clone.HardwareOC = null;
            return clone;
        }

        object ICloneable.Clone() => Clone();
        IModuleData IModuleData.Clone() => Clone();
    }
}
