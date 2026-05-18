using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using SSS.Core;

namespace SSS.Module.NetworkMonitor
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

        private byte _order = 1;

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
            set { _metrics = value; NotifyPropertyChanged(); }
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
            set { _showHardwareNames = value; NotifyPropertyChanged(); }
        }

        private bool _roundAll = false;

        [JsonProperty("roundAll")]
        public bool RoundAll
        {
            get => _roundAll;
            set { _roundAll = value; NotifyPropertyChanged(); }
        }

        private bool _useBytes = false;

        [JsonProperty("useBytes")]
        public bool UseBytes
        {
            get => _useBytes;
            set { _useBytes = value; NotifyPropertyChanged(); }
        }

        private int _bandwidthInAlert = 0;

        [JsonProperty("bandwidthInAlert")]
        public int BandwidthInAlert
        {
            get => _bandwidthInAlert;
            set { _bandwidthInAlert = value; NotifyPropertyChanged(); }
        }

        private int _bandwidthOutAlert = 0;

        [JsonProperty("bandwidthOutAlert")]
        public int BandwidthOutAlert
        {
            get => _bandwidthOutAlert;
            set { _bandwidthOutAlert = value; NotifyPropertyChanged(); }
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
