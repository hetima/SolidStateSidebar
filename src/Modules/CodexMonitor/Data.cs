using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using SSS.Core;

namespace SSS.Module.CodexMonitor
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

        private HardwareConfig[] _hardware =
        [
            new HardwareConfig() { ID = "codex", Name = "Codex", ActualName = "Codex" }
        ];

        [JsonProperty("hardware")]
        public HardwareConfig[] Hardware
        {
            get => _hardware;
            set { _hardware = value; NotifyPropertyChanged(); }
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
            set { _sectionHeaderStyle = value; NotifyPropertyChanged(); }
        }

        private AutoRefreshInterval _autoRefresh = AutoRefreshInterval.TenMin;

        [JsonProperty("autoRefresh")]
        public AutoRefreshInterval AutoRefresh
        {
            get => _autoRefresh;
            set { _autoRefresh = value; NotifyPropertyChanged(); }
        }

        private ResetTimeDisplay _shortResetDisplay = ResetTimeDisplay.Absolute;

        [JsonProperty("shortResetDisplay")]
        public ResetTimeDisplay ShortResetDisplay
        {
            get => _shortResetDisplay;
            set { _shortResetDisplay = value; NotifyPropertyChanged(); }
        }

        private ResetTimeDisplay _longResetDisplay = ResetTimeDisplay.Countdown;

        [JsonProperty("longResetDisplay")]
        public ResetTimeDisplay LongResetDisplay
        {
            get => _longResetDisplay;
            set { _longResetDisplay = value; NotifyPropertyChanged(); }
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
