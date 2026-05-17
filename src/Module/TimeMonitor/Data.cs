using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using SSS.Core;

namespace SSS.Module.TimeMonitor
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

        private byte _order = 6;

        [JsonProperty("order")]
        public byte Order
        {
            get => _order;
            set { _order = value; NotifyPropertyChanged(); }
        }

        private HardwareConfig[] _hardware =
        [
            new HardwareConfig() { ID = "clock", Name = Strings.Time, ActualName = Strings.Time }
        ];

        [JsonProperty("hardware")]
        public HardwareConfig[] Hardware
        {
            get => _hardware;
            set { _hardware = value; NotifyPropertyChanged(); }
        }

        // --- IModuleData (not used by TimeMonitor, empty for interface compliance) ---

        [JsonIgnore]
        public MetricConfig[] Metrics { get; set; } = [];

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
            set { _showDate = value; NotifyPropertyChanged(); }
        }

        private bool _showTime = true;

        [JsonProperty("showTime")]
        public bool ShowTime
        {
            get => _showTime;
            set { _showTime = value; NotifyPropertyChanged(); }
        }

        private bool _clock24HR = false;

        [JsonProperty("clock24HR")]
        public bool Clock24HR
        {
            get => _clock24HR;
            set { _clock24HR = value; NotifyPropertyChanged(); }
        }

        private int _dateFormat = 2;

        [JsonProperty("dateFormat")]
        public int DateFormat
        {
            get => _dateFormat;
            set { _dateFormat = value; NotifyPropertyChanged(); }
        }

        private int _dateFontSize = 14;

        [JsonProperty("dateFontSize")]
        public int DateFontSize
        {
            get => _dateFontSize;
            set { _dateFontSize = value; NotifyPropertyChanged(); }
        }

        private int _timeFontSize = 12;

        [JsonProperty("timeFontSize")]
        public int TimeFontSize
        {
            get => _timeFontSize;
            set { _timeFontSize = value; NotifyPropertyChanged(); }
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
