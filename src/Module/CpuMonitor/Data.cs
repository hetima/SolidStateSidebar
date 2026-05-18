using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using SSS.Core;

namespace SSS.Module.CpuMonitor
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

        private byte _order = 5;

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
        public string Name => Strings.CPU;

        [JsonIgnore]
        public ObservableCollection<HardwareConfig>? HardwareOC { get; set; }

        // --- CpuMonitor-specific params ---

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

        private bool _allCoreClocks = false;

        [JsonProperty("allCoreClocks")]
        public bool AllCoreClocks
        {
            get => _allCoreClocks;
            set { _allCoreClocks = value; NotifyPropertyChanged(); }
        }

        private bool _useGHz = false;

        [JsonProperty("useGHz")]
        public bool UseGHz
        {
            get => _useGHz;
            set { _useGHz = value; NotifyPropertyChanged(); }
        }

        private bool _useFahrenheit = false;

        [JsonProperty("useFahrenheit")]
        public bool UseFahrenheit
        {
            get => _useFahrenheit;
            set { _useFahrenheit = value; NotifyPropertyChanged(); }
        }

        private int _tempAlert = 0;

        [JsonProperty("tempAlert")]
        public int TempAlert
        {
            get => _tempAlert;
            set { _tempAlert = value; NotifyPropertyChanged(); }
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
            Order = 5,
            Hardware = [],
            Metrics =
            [
                new MetricConfig(MetricKey.CPUClock, true),
                new MetricConfig(MetricKey.CPUTemp, true),
                new MetricConfig(MetricKey.CPUVoltage, true),
                new MetricConfig(MetricKey.CPUFan, true),
                new MetricConfig(MetricKey.CPULoad, true),
                new MetricConfig(MetricKey.CPUCoreLoad, true)
            ],
            ShowHardwareNames = true,
            RoundAll = false,
            AllCoreClocks = false,
            UseGHz = false,
            UseFahrenheit = false,
            TempAlert = 0
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
