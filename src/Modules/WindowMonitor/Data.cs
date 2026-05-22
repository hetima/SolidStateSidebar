using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using SSS.Core;

namespace SSS.Module.WindowMonitor
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

        private byte _order = 7;

        [JsonProperty("order")]
        public byte Order
        {
            get => _order;
            set { _order = value; NotifyPropertyChanged(); }
        }

        private HardwareConfig[] _hardware =
        [
            new HardwareConfig() { ID = "window", Name = "Window", ActualName = "Window" }
        ];

        [JsonProperty("hardware")]
        public HardwareConfig[] Hardware
        {
            get => _hardware;
            set { _hardware = value; NotifyPropertyChanged(); }
        }

        private HardwareConfig[] _applications = [];

        [JsonProperty("applications")]
        public HardwareConfig[] Applications
        {
            get => _applications;
            set { _applications = value; NotifyPropertyChanged(); }
        }

        // --- IModuleData (not used by WindowMonitor, empty for interface compliance) ---

        [JsonIgnore]
        public ObservableCollection<MetricConfig> Metrics { get; set; } = [];

        // --- UI-only (not serialized) ---

        [JsonIgnore]
        public string Name => Strings.Window;

        [JsonIgnore]
        public ObservableCollection<HardwareConfig>? HardwareOC { get; set; }

        [JsonIgnore]
        public ObservableCollection<HardwareConfig>? ApplicationOC { get; set; }

        // --- WindowMonitor-specific params ---

        private SectionHeaderStyle _sectionHeaderStyle = SectionHeaderStyle.Default;

        [JsonProperty("sectionHeaderStyle")]
        public SectionHeaderStyle SectionHeaderStyle
        {
            get => _sectionHeaderStyle;
            set { _sectionHeaderStyle = value; NotifyPropertyChanged(); }
        }

        private int _maxDisplayCount = 8;

        [JsonProperty("maxDisplayCount")]
        public int MaxDisplayCount
        {
            get => _maxDisplayCount;
            set { _maxDisplayCount = value; NotifyPropertyChanged(); }
        }

        // --- Defaults & Clone ---

        public static Data Default => new Data
        {
            Enabled = true,
            Order = 7,
            Hardware =
            [
                new HardwareConfig() { ID = "window", Name = "Window", ActualName = "Window" }
            ],
            Applications = [],
            SectionHeaderStyle = SectionHeaderStyle.Default,
            MaxDisplayCount = 8
        };

        public Data Clone()
        {
            Data clone = (Data)MemberwiseClone();
            clone.Hardware = clone.Hardware.Select(h => h.Clone()).ToArray();
            clone.Applications = clone.Applications.Select(a => a.Clone()).ToArray();
            clone.Metrics = [];
            clone.HardwareOC = null;
            clone.ApplicationOC = null;
            return clone;
        }

        object ICloneable.Clone() => Clone();
        IModuleData IModuleData.Clone() => Clone();
    }
}
