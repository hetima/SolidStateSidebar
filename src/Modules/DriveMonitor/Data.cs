using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using SSS.Core;

namespace SSS.Module.HdMonitor
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

        private byte _order = 2;

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
        public string Name => Strings.Drives;

        [JsonIgnore]
        public ObservableCollection<HardwareConfig>? HardwareOC { get; set; }

        // --- HdMonitor-specific params ---

        private bool _roundAll = false;

        [JsonProperty("roundAll")]
        public bool RoundAll
        {
            get => _roundAll;
            set => SetProperty(ref _roundAll, value);
        }

        private int _usedSpaceAlert = 0;

        [JsonProperty("usedSpaceAlert")]
        public int UsedSpaceAlert
        {
            get => _usedSpaceAlert;
            set => SetProperty(ref _usedSpaceAlert, value);
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
            Order = 2,
            Hardware = [],
            Metrics = new ObservableCollection<MetricConfig>(
            [
                new MetricConfig(MetricKey.DriveLoadBar, true),
                new MetricConfig(MetricKey.DriveLoad, true),
                new MetricConfig(MetricKey.DriveUsed, true),
                new MetricConfig(MetricKey.DriveFree, true),
                new MetricConfig(MetricKey.DriveRead, true),
                new MetricConfig(MetricKey.DriveWrite, true)
            ]),
            RoundAll = false,
            UsedSpaceAlert = 0
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
