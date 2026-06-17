using System;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using SSS.Core;

namespace SSS.Module.TimeMonitor
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

        private byte _order = 6;

        [JsonProperty("order")]
        public byte Order
        {
            get => _order;
            set => SetProperty(ref _order, value);
        }

        private HardwareConfig[] _hardware =
        [
            new HardwareConfig() { ID = "clock", Name = Strings.Time, ActualName = Strings.Time }
        ];

        [JsonProperty("hardware")]
        public HardwareConfig[] Hardware
        {
            get => _hardware;
            set => SetProperty(ref _hardware, value);
        }

        // --- IModuleData (not used by TimeMonitor, empty for interface compliance) ---

        [JsonIgnore]
        public ObservableCollection<MetricConfig> Metrics { get; set; } = [];

        // --- UI-only (not serialized) ---

        [JsonIgnore]
        public string Name => Strings.Time;

        [JsonIgnore]
        public ObservableCollection<HardwareConfig>? HardwareOC { get; set; }

        // --- TimeMonitor-specific params ---

        private bool _showDate = true;

        [JsonProperty("showDate")]
        public bool ShowDate
        {
            get => _showDate;
            set => SetProperty(ref _showDate, value);
        }

        private bool _showTime = true;

        [JsonProperty("showTime")]
        public bool ShowTime
        {
            get => _showTime;
            set => SetProperty(ref _showTime, value);
        }

        private bool _clock24HR = false;

        [JsonProperty("clock24HR")]
        public bool Clock24HR
        {
            get => _clock24HR;
            set => SetProperty(ref _clock24HR, value);
        }

        private int _dateFormat = 2;

        [JsonProperty("dateFormat")]
        public int DateFormat
        {
            get => _dateFormat;
            set => SetProperty(ref _dateFormat, value);
        }

        private bool _showDayOfWeek = false;

        [JsonProperty("showDayOfWeek")]
        public bool ShowDayOfWeek
        {
            get => _showDayOfWeek;
            set => SetProperty(ref _showDayOfWeek, value);
        }

        private int _dateFontSize = 14;

        [JsonProperty("dateFontSize")]
        public int DateFontSize
        {
            get => _dateFontSize;
            set => SetProperty(ref _dateFontSize, value);
        }

        private int _timeFontSize = 12;

        [JsonProperty("timeFontSize")]
        public int TimeFontSize
        {
            get => _timeFontSize;
            set => SetProperty(ref _timeFontSize, value);
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
            Order = 6,
            Hardware =
            [
                new HardwareConfig() { ID = "clock", Name = Strings.Time, ActualName = Strings.Time }
            ],
            ShowDate = true,
            ShowTime = true,
            Clock24HR = false,
            DateFormat = 2,
            DateFontSize = 14,
            TimeFontSize = 12
        };

        public Data Clone()
        {
            Data clone = (Data)MemberwiseClone();
            clone.Hardware = clone.Hardware.Select(h => h.Clone()).ToArray();
            clone.Metrics = [];
            clone.HardwareOC = null;
            return clone;
        }

        object ICloneable.Clone() => Clone();
        IModuleData IModuleData.Clone() => Clone();
    }
}
