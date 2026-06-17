using System;
using Newtonsoft.Json;

namespace SSS.Core
{
    [JsonObject(MemberSerialization.OptIn)]
    public class HardwareConfig : ObservableObject, ICloneable
    {
        public HardwareConfig Clone()
        {
            return (HardwareConfig)MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private string? _id;

        [JsonProperty]
        public string? ID
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string? _name;

        [JsonProperty]
        public string? Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string? _actualName;

        [JsonProperty]
        public string? ActualName
        {
            get => _actualName;
            set => SetProperty(ref _actualName, value);
        }

        private bool _enabled = true;

        [JsonProperty]
        public bool Enabled
        {
            get => _enabled;
            set => SetProperty(ref _enabled, value);
        }

        private byte _order = 0;

        [JsonProperty]
        public byte Order
        {
            get => _order;
            set => SetProperty(ref _order, value);
        }
    }
}
