using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace SSS.Core
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MetricConfig : INotifyPropertyChanged, ICloneable
    {
        public MetricConfig() { }

        public MetricConfig(MetricKey key, bool enabled)
        {
            Key = key;
            Enabled = enabled;
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public MetricConfig Clone()
        {
            return (MetricConfig)MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private MetricKey _key { get; set; }

        [JsonProperty]
        public MetricKey Key
        {
            get
            {
                return _key;
            }
            set
            {
                _key = value;

                NotifyPropertyChanged(nameof(Key));
            }
        }

        private bool _enabled { get; set; }

        [JsonProperty]
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if (_enabled == value)
                {
                    return;
                }

                _enabled = value;

                NotifyPropertyChanged(nameof(Enabled));
            }
        }

        public string Name
        {
            get
            {
                return Key.GetFullName();
            }
        }

        public string ActualLabel
        {
            get
            {
                return Key.GetLabel();
            }
        }

        private string? _label { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Label
        {
            get
            {
                return _label;
            }
            set
            {
                _label = value;

                NotifyPropertyChanged(nameof(Label));
            }
        }

        private byte _order { get; set; }

        [JsonProperty]
        public byte Order
        {
            get
            {
                return _order;
            }
            set
            {
                _order = value;

                NotifyPropertyChanged(nameof(Order));
            }
        }
    }
}
