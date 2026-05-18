using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using SSS.Core;
using SSS.Windows;

namespace SSS.Converters
{
    public class IntToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string _format = (string)parameter;

            if (string.IsNullOrEmpty(_format))
            {
                return value.ToString() ?? "";
            }
            else
            {
                return string.Format(culture, _format, value);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int _return = 0;

            int.TryParse(value.ToString(), out _return);

            return _return;
        }
    }

    public class HotkeyToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Hotkey _hotkey = (Hotkey)value;

            if (_hotkey == null)
            {
                return "None";
            }

            return
                (_hotkey.AltMod ? "Alt + " : "") +
                (_hotkey.CtrlMod ? "Ctrl + " : "") +
                (_hotkey.ShiftMod ? "Shift + " : "") +
                (_hotkey.WinMod ? "Win + " : "") +
                new KeyConverter().ConvertToString(_hotkey.WinKey);
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class PercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double _value = (double)value;

            return string.Format("{0:0}%", _value);
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class BoolInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }
    }

    public class MetricLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string _value = (string)value;

            if (string.IsNullOrEmpty(_value))
            {
                return string.Empty;
            }

            return string.Format("{0}:", _value);
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class MetricLabelDisplayConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[1] is string actualLabel)
            {
                string? label = values[0] as string;
                return label ?? actualLabel ?? string.Empty;
            }
            return string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return [value ?? string.Empty];
        }
    }

    public class EnabledMetricsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<MetricConfig> metrics)
            {
                return string.Join(", ", metrics.Where(m => m.Enabled).Select(m => m.Name));
            }
            return string.Empty;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class FontToSpaceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int _value = (int)value;

            return new Thickness(0, 0, _value * 0.4d, 0);
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class DateFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToInt32(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToInt32(value);
        }
    }

    public class DateFormatDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || (value is string s && string.IsNullOrEmpty(s)))
                return Strings.DateDisabled;

            int intValue = System.Convert.ToInt32(value);

            return intValue switch
            {
                0 => Strings.DateDisabled,
                1 => Strings.DateShort,
                2 => Strings.DateNormal,
                3 => Strings.DateLong,
                _ => intValue.ToString()
            };
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class SectionHeaderStyleDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Core.SectionHeaderStyle style)
            {
                return style switch
                {
                    Core.SectionHeaderStyle.Default => Strings.SettingsHeaderStyleDefault,
                    Core.SectionHeaderStyle.Small => Strings.SettingsHeaderStyleSmall,
                    Core.SectionHeaderStyle.None => Strings.SettingsHeaderStyleNone,
                    _ => style.ToString()
                };
            }
            return value?.ToString() ?? "";
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class FontSizeAddConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double baseSize = System.Convert.ToDouble(value);
            if (double.TryParse(parameter?.ToString(), out double add))
            {
                return baseSize + add;
            }
            return value;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class IntToThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i)
            {
                return new Thickness(i, 0, i, 0);
            }
            if (value is double d)
            {
                return new Thickness(d, 0, d, 0);
            }
            return new Thickness(0);
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
