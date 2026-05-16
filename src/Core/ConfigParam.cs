using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace SSS.Core
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ConfigParam : INotifyPropertyChanged, ICloneable
    {
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ConfigParam Clone()
        {
            return (ConfigParam)MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private ParamKey _key { get; set; }

        [JsonProperty]
        public ParamKey Key
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

        private object? _value { get; set; }

        [JsonProperty]
        public object? Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (value?.GetType() == typeof(long))
                {
                    _value = Convert.ToInt32(value);
                }
                else
                {
                    _value = value;
                }

                NotifyPropertyChanged(nameof(Value));
            }
        }

        public Type? Type
        {
            get
            {
                return Value?.GetType();
            }
        }

        public string TypeString
        {
            get
            {
                return Type?.ToString() ?? string.Empty;
            }
        }

        public string Name
        {
            get
            {
                switch (Key)
                {
                    case ParamKey.HardwareNames:
                        return Strings.SettingsShowHardwareNames;

                    case ParamKey.UseFahrenheit:
                        return Strings.SettingsUseFahrenheit;

                    case ParamKey.AllCoreClocks:
                        return Strings.SettingsAllCoreClocks;

                    case ParamKey.CoreLoads:
                        return Strings.SettingsCoreLoads;

                    case ParamKey.TempAlert:
                        return Strings.SettingsTemperatureAlert;

                    case ParamKey.DriveDetails:
                        return Strings.SettingsShowDriveDetails;

                    case ParamKey.UsedSpaceAlert:
                        return Strings.SettingsUsedSpaceAlert;

                    case ParamKey.BandwidthInAlert:
                        return Strings.SettingsBandwidthInAlert;

                    case ParamKey.BandwidthOutAlert:
                        return Strings.SettingsBandwidthOutAlert;

                    case ParamKey.UseBytes:
                        return Strings.SettingsUseBytesPerSecond;

                    case ParamKey.RoundAll:
                        return Strings.SettingsRoundAllDecimals;

                    case ParamKey.DriveSpace:
                        return Strings.SettingsShowDriveSpace;

                    case ParamKey.DriveIO:
                        return Strings.SettingsShowDriveIO;

                    case ParamKey.UseGHz:
                        return Strings.SettingsUseGHz;

                    default:
                        return "Unknown";
                }
            }
        }

        public string Tooltip
        {
            get
            {
                switch (Key)
                {
                    case ParamKey.HardwareNames:
                        return Strings.SettingsShowHardwareNamesTooltip;

                    case ParamKey.UseFahrenheit:
                        return Strings.SettingsUseFahrenheitTooltip;

                    case ParamKey.AllCoreClocks:
                        return Strings.SettingsAllCoreClocksTooltip;

                    case ParamKey.CoreLoads:
                        return Strings.SettingsCoreLoadsTooltip;

                    case ParamKey.TempAlert:
                        return Strings.SettingsTemperatureAlertTooltip;

                    case ParamKey.DriveDetails:
                        return Strings.SettingsDriveDetailsTooltip;

                    case ParamKey.UsedSpaceAlert:
                        return Strings.SettingsUsedSpaceAlertTooltip;

                    case ParamKey.BandwidthInAlert:
                        return Strings.SettingsBandwidthInAlertTooltip;

                    case ParamKey.BandwidthOutAlert:
                        return Strings.SettingsBandwidthOutAlertTooltip;

                    case ParamKey.UseBytes:
                        return Strings.SettingsUseBytesPerSecondTooltip;

                    case ParamKey.RoundAll:
                        return Strings.SettingsRoundAllDecimalsTooltip;

                    case ParamKey.DriveSpace:
                        return Strings.SettingsShowDriveSpaceTooltip;

                    case ParamKey.DriveIO:
                        return Strings.SettingsShowDriveIOTooltip;

                    case ParamKey.UseGHz:
                        return Strings.SettingsUseGHzTooltip;

                    default:
                        return "Unknown";
                }
            }
        }

        public static class Defaults
        {
            public static ConfigParam HardwareNames
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.HardwareNames, Value = true };
                }
            }

            public static ConfigParam NoHardwareNames
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.HardwareNames, Value = false };
                }
            }

            public static ConfigParam UseFahrenheit
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.UseFahrenheit, Value = false };
                }
            }

            public static ConfigParam AllCoreClocks
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.AllCoreClocks, Value = false };
                }
            }

            public static ConfigParam CoreLoads
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.CoreLoads, Value = true };
                }
            }

            public static ConfigParam TempAlert
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.TempAlert, Value = 0 };
                }
            }

            public static ConfigParam DriveDetails
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.DriveDetails, Value = false };
                }
            }

            public static ConfigParam UsedSpaceAlert
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.UsedSpaceAlert, Value = 0 };
                }
            }

            public static ConfigParam BandwidthInAlert
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.BandwidthInAlert, Value = 0 };
                }
            }

            public static ConfigParam BandwidthOutAlert
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.BandwidthOutAlert, Value = 0 };
                }
            }

            public static ConfigParam UseBytes
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.UseBytes, Value = false };
                }
            }

            public static ConfigParam RoundAll
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.RoundAll, Value = false };
                }
            }

            public static ConfigParam ShowDriveSpace
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.DriveSpace, Value = true };
                }
            }

            public static ConfigParam ShowDriveIO
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.DriveIO, Value = true };
                }
            }

            public static ConfigParam UseGHz
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.UseGHz, Value = false };
                }
            }
        }
    }
}
