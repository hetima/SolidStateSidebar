using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using SSS.Core;

namespace SSS.Module.RamMonitor
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

        private byte _order = 4;

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

        private MetricConfig[] _metrics = [];

        [JsonProperty("metrics")]
        public MetricConfig[] Metrics
        {
            get => _metrics;
            set { _metrics = value; NotifyPropertyChanged(); }
        }

        // --- UI-only (not serialized) ---

        [JsonIgnore]
        public string Name => Strings.RAM;

        [JsonIgnore]
        public ObservableCollection<HardwareConfig>? HardwareOC { get; set; }

        // --- RamMonitor-specific params ---

        private bool _roundAll = false;

        [JsonProperty("roundAll")]
        public bool RoundAll
        {
            get => _roundAll;
            set { _roundAll = value; NotifyPropertyChanged(); }
        }

        // --- Defaults & Clone ---

        public static Data Default => new Data
        {
            Enabled = true,
            Order = 4,
            Hardware = [],
            Metrics =
            [
                new MetricConfig(MetricKey.RAMClock, true),
                new MetricConfig(MetricKey.RAMVoltage, true),
                new MetricConfig(MetricKey.RAMLoad, true),
                new MetricConfig(MetricKey.RAMUsed, true),
                new MetricConfig(MetricKey.RAMFree, true)
            ],
            RoundAll = false
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
