using System;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using SSS.Core;

namespace SSS.Module.WindowMonitor
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

        private byte _order = 7;

        [JsonProperty("order")]
        public byte Order
        {
            get => _order;
            set => SetProperty(ref _order, value);
        }

        private HardwareConfig[] _hardware =
        [
            new HardwareConfig() { ID = "window", Name = "Window", ActualName = "Window" }
        ];

        [JsonProperty("hardware")]
        public HardwareConfig[] Hardware
        {
            get => _hardware;
            set => SetProperty(ref _hardware, value);
        }

        private HardwareConfig[] _applications = [];

        [JsonProperty("applications")]
        public HardwareConfig[] Applications
        {
            get => _applications;
            set => SetProperty(ref _applications, value);
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
            set => SetProperty(ref _sectionHeaderStyle, value);
        }

        private int _maxDisplayCount = 8;

        [JsonProperty("maxDisplayCount")]
        public int MaxDisplayCount
        {
            get => _maxDisplayCount;
            set => SetProperty(ref _maxDisplayCount, value);
        }

        private int _fontSize = 0;

        [JsonProperty("fontSize")]
        public int FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }

        private string? _fontName = null;

        [JsonProperty("fontName")]
        public string? FontName
        {
            get => _fontName;
            set => SetProperty(ref _fontName, value);
        }

        private bool _scrollToSwitch = false;

        [JsonProperty("scrollToSwitch")]
        public bool ScrollToSwitch
        {
            get => _scrollToSwitch;
            set => SetProperty(ref _scrollToSwitch, value);
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
            MaxDisplayCount = 8,
            FontSize = 0,
            FontName = null,
            ScrollToSwitch = false
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
