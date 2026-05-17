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
    }
}
