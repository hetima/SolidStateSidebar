using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using SSS.Core;

namespace SSS.Module.RamMonitor
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

        private byte _order = 4;

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
        public string Name => Strings.RAM;

        [JsonIgnore]
        public ObservableCollection<HardwareConfig>? HardwareOC { get; set; }

        // --- RamMonitor-specific params ---

        private bool _roundAll = false;

        [JsonProperty("roundAll")]
        public bool RoundAll
        {
            get => _roundAll;
            set => SetProperty(ref _roundAll, value);
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
            Order = 4,
            Hardware = [],
            Metrics = new ObservableCollection<MetricConfig>(
            [
                new MetricConfig(MetricKey.RAMClock, true),
                new MetricConfig(MetricKey.RAMVoltage, true),
                new MetricConfig(MetricKey.RAMLoad, true),
                new MetricConfig(MetricKey.RAMUsed, true),
                new MetricConfig(MetricKey.RAMFree, true)
            ]),
            RoundAll = false
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
