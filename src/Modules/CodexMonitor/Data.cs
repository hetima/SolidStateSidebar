using System;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using SSS.Core;

namespace SSS.Module.CodexMonitor
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

        private HardwareConfig[] _hardware =
        [
            new HardwareConfig() { ID = "codex", Name = "Codex", ActualName = "Codex" }
        ];

        [JsonProperty("hardware")]
        public HardwareConfig[] Hardware
        {
            get => _hardware;
            set => SetProperty(ref _hardware, value);
        }

        [JsonIgnore]
        public ObservableCollection<MetricConfig> Metrics { get; set; } = [];

        [JsonIgnore]
        public string Name => "Codex";

        [JsonIgnore]
        public ObservableCollection<HardwareConfig>? HardwareOC { get; set; }

        // --- 表示設定 ---

        private SectionHeaderStyle _sectionHeaderStyle = SectionHeaderStyle.Default;

        [JsonProperty("sectionHeaderStyle")]
        public SectionHeaderStyle SectionHeaderStyle
        {
            get => _sectionHeaderStyle;
            set => SetProperty(ref _sectionHeaderStyle, value);
        }

        private AutoRefreshInterval _autoRefresh = AutoRefreshInterval.TenMin;

        [JsonProperty("autoRefresh")]
        public AutoRefreshInterval AutoRefresh
        {
            get => _autoRefresh;
            set => SetProperty(ref _autoRefresh, value);
        }

        private ResetTimeDisplay _shortResetDisplay = ResetTimeDisplay.Absolute;

        [JsonProperty("shortResetDisplay")]
        public ResetTimeDisplay ShortResetDisplay
        {
            get => _shortResetDisplay;
            set => SetProperty(ref _shortResetDisplay, value);
        }

        private ResetTimeDisplay _longResetDisplay = ResetTimeDisplay.Countdown;

        [JsonProperty("longResetDisplay")]
        public ResetTimeDisplay LongResetDisplay
        {
            get => _longResetDisplay;
            set => SetProperty(ref _longResetDisplay, value);
        }

        // --- Defaults & Clone ---

        public static Data Default => new Data
        {
            Enabled = false,
            Order = 1,
            Hardware =
            [
                new HardwareConfig() { ID = "codex", Name = "Codex", ActualName = "Codex" }
            ]
        };

        public Data Clone()
        {
            Data clone = (Data)MemberwiseClone();
            clone.Hardware = [.. clone.Hardware];
            clone.Metrics = [];
            clone.HardwareOC = null;
            return clone;
        }

        object ICloneable.Clone() => Clone();
        IModuleData IModuleData.Clone() => Clone();
    }
}
