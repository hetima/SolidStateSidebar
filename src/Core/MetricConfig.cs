using System;
using Newtonsoft.Json;

namespace SSS.Core
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MetricConfig : ObservableObject, ICloneable
    {
        public MetricConfig() { }

        public MetricConfig(MetricKey key, bool enabled)
        {
            Key = key;
            Enabled = enabled;
        }

        public MetricConfig Clone()
        {
            return (MetricConfig)MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private MetricKey _key;

        [JsonProperty]
        public MetricKey Key
        {
            get => _key;
            set => SetProperty(ref _key, value);
        }

        private bool _enabled;

        [JsonProperty]
        public bool Enabled
        {
            get => _enabled;
            set => SetProperty(ref _enabled, value);
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

        private string? _label;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Label
        {
            get => _label;
            set => SetProperty(ref _label, value);
        }

        private byte _order;

        [JsonProperty]
        public byte Order
        {
            get => _order;
            set => SetProperty(ref _order, value);
        }
    }
}
