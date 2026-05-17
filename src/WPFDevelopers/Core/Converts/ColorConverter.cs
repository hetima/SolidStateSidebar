using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Media;
using WPFDevelopers.Utilities;

namespace WPFDevelopers.Converts
{
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        { return new SolidColorBrush((Color)value); }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public partial class ColorToStringConverter : IValueConverter
    {
        private Color? _curColor;

        [GeneratedRegex(@"^#?[\da-fA-F]{6}$")]
        private static partial Regex HexColorRegex();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color c)
            {
                _curColor = c;
                return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            }
            if (value is string s && !string.IsNullOrWhiteSpace(s))
            {
                _curColor = (Color)ColorConverter.ConvertFromString(s);
                return $"#{_curColor.Value.R:X2}{_curColor.Value.G:X2}{_curColor.Value.B:X2}";
            }
            return "#000000";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s) && HexColorRegex().IsMatch(s))
            {
                _curColor = (Color)ColorConverter.ConvertFromString(s.StartsWith("#") ? s : "#" + s);
                return _curColor.Value;
            }
            return _curColor ?? Colors.Black;
        }
    }

    public class StringToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s))
                return (Color)ColorConverter.ConvertFromString(s);
            return Colors.Black;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color c)
                return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            return "#000000";
        }
    }

}
