using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using SSS.Core;

namespace SSS.Module.NetworkMonitor
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

        private byte _order = 1;

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
        public string Name => Strings.Network;

        [JsonIgnore]
        public ObservableCollection<HardwareConfig>? HardwareOC { get; set; }

        // --- NetworkMonitor-specific params ---

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

        private bool _useBytes = false;

        [JsonProperty("useBytes")]
        public bool UseBytes
        {
            get => _useBytes;
            set => SetProperty(ref _useBytes, value);
        }

        private int _bandwidthInAlert = 0;

        [JsonProperty("bandwidthInAlert")]
        public int BandwidthInAlert
        {
            get => _bandwidthInAlert;
            set => SetProperty(ref _bandwidthInAlert, value);
        }

        private int _bandwidthOutAlert = 0;

        [JsonProperty("bandwidthOutAlert")]
        public int BandwidthOutAlert
        {
            get => _bandwidthOutAlert;
            set => SetProperty(ref _bandwidthOutAlert, value);
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
            Order = 1,
            Hardware = [],
            Metrics = new ObservableCollection<MetricConfig>(
            [
                new MetricConfig(MetricKey.NetworkIP, true),
                new MetricConfig(MetricKey.NetworkExtIP, false),
                new MetricConfig(MetricKey.NetworkIn, true),
                new MetricConfig(MetricKey.NetworkOut, true)
            ]),
            ShowHardwareNames = true,
            RoundAll = false,
            UseBytes = false,
            BandwidthInAlert = 0,
            BandwidthOutAlert = 0
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
