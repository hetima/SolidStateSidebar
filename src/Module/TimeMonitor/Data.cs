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

        private MetricConfig[] _metrics = [];

        [JsonProperty("metrics")]
        public MetricConfig[] Metrics
        {
            get => _metrics;
            set { _metrics = value; NotifyPropertyChanged(); }
        }

        // --- UI-only (not serialized) ---

        [JsonIgnore]
        public string Name => Strings.Time;

        [JsonIgnore]
        public ObservableCollection<HardwareConfig>? HardwareOC { get; set; }

        // --- TimeMonitor-specific params ---

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

        // --- Defaults & Clone ---

        public static Data Default => new Data
        {
            Enabled = true,
            Order = 6,
            Hardware =
            [
                new HardwareConfig() { ID = "clock", Name = Strings.Time, ActualName = Strings.Time }
            ],
            Metrics =
            [
                new MetricConfig(MetricKey.Time, true),
                new MetricConfig(MetricKey.Date, true)
            ],
            Clock24HR = false,
            DateFormat = 2
        };

        public Data Clone()
        {
            Data clone = (Data)MemberwiseClone();
            clone.Hardware = clone.Hardware.Select(h => h.Clone()).ToArray();
            clone.Metrics = clone.Metrics.Select(m => m.Clone()).ToArray();
            clone.HardwareOC = null;
            return clone;
        }

        object ICloneable.Clone() => Clone();
        IModuleData IModuleData.Clone() => Clone();
    }
}
