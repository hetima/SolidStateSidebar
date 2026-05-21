using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using SSS.Core;

namespace SSS.Module.HdMonitor
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Data : Core.IModuleData, ICloneable
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // --- IModuleData ---

        private bool _enabled = true;

        [JsonProperty("enabled")]
        public bool Enabled
        {
            get => _enabled;
            set { _enabled = value; NotifyPropertyChanged(); }
        }

        private byte _order = 2;

        [JsonProperty("order")]
        public byte Order
        {
            get => _order;
            set { _order = value; NotifyPropertyChanged(); }
        }

        private HardwareConfig[] _hardware = [];

        [JsonProperty("hardware")]
        public HardwareConfig[] Hardware
        {
            get => _hardware;
            set { _hardware = value; NotifyPropertyChanged(); }
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
            set { _roundAll = value; NotifyPropertyChanged(); }
        }

        private int _usedSpaceAlert = 0;

        [JsonProperty("usedSpaceAlert")]
        public int UsedSpaceAlert
        {
            get => _usedSpaceAlert;
            set { _usedSpaceAlert = value; NotifyPropertyChanged(); }
        }

        private SectionHeaderStyle _sectionHeaderStyle = SectionHeaderStyle.Default;

        [JsonProperty("sectionHeaderStyle")]
        public SectionHeaderStyle SectionHeaderStyle
        {
            get => _sectionHeaderStyle;
            set { _sectionHeaderStyle = value; NotifyPropertyChanged(); }
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
