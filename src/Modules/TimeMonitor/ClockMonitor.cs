using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SSS.Core;
using SSS.Utilities;

namespace SSS.Module.TimeMonitor
{
    public class ClockTimeMetric : BaseMetric
    {
        private readonly bool _clock24HR;
        private int _fontSize;

        public ClockTimeMetric(bool clock24HR, int fontSize) : base(MetricKey.Time, DataType.Dynamic, "")
        {
            _clock24HR = clock24HR;
            _fontSize = fontSize;
        }

        public int FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    NotifyPropertyChanged(nameof(FontSize));
                }
            }
        }

        ~ClockTimeMetric()
        {
            Dispose(false);
        }

        public override bool IsNumeric
        {
            get { return false; }
        }

        public override void Update()
        {
            DateTime _now = DateTime.Now;

            Text = _now.ToString(_clock24HR ? "H:mm:ss" : "h:mm:ss tt", Culture.Default);
        }
    }

    public class ClockDateMetric : BaseMetric
    {
        private readonly string _format;
        private readonly bool _showDayOfWeek;
        private int _fontSize;

        public ClockDateMetric(string format, int fontSize, bool showDayOfWeek) : base(MetricKey.Date, DataType.Dynamic, "")
        {
            _format = format;
            _fontSize = fontSize;
            _showDayOfWeek = showDayOfWeek;
        }

        public int FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    NotifyPropertyChanged(nameof(FontSize));
                }
            }
        }

        ~ClockDateMetric()
        {
            Dispose(false);
        }

        public override bool IsNumeric
        {
            get { return false; }
        }

        public override void Update()
        {
            DateTime _now = DateTime.Now;
            string _nowTwxt = string.IsNullOrEmpty(_format) ? "" : _now.ToString(_format, Culture.Default);

            if (_showDayOfWeek)
            {
                Text = $"{_nowTwxt} ({_now.ToString("ddd", Culture.Default)})";
            }else{
                Text = _nowTwxt;
            }

        }
    }

    public class ClockMonitor : BaseMonitor
    {
        public ClockMonitor(bool showDate, bool showTime, bool clock24HR, int dateFormatValue, bool showDayOfWeek, int dateFontSize, int timeFontSize) : base("clock", Strings.Time, false)
        {
            List<iMetric> _metrics = [];

            if (showDate)
            {
                string _format = GetDateFormat(dateFormatValue);

                _metrics.Add(new ClockDateMetric(_format, dateFontSize, showDayOfWeek));
            }

            if (showTime)
            {
                _metrics.Add(new ClockTimeMetric(clock24HR, timeFontSize));
            }

            Metrics = _metrics.ToArray();
        }

        ~ClockMonitor()
        {
            Dispose(false);
        }

        public static IEnumerable<HardwareConfig> GetHardware()
        {
            return [new HardwareConfig() { ID = "clock", Name = Strings.Time, ActualName = Strings.Time }];
        }

        public static iMonitor[] GetInstances(HardwareConfig[] hardwareConfig, bool showDate, bool showTime, bool clock24HR, int dateFormatValue, bool showDayOfWeek, int dateFontSize, int timeFontSize)
        {
            return
            [
                new ClockMonitor(showDate, showTime, clock24HR, dateFormatValue, showDayOfWeek, dateFontSize, timeFontSize)
            ];
        }

        private static string GetDateFormat(int value)
        {
            return value switch
            {
                0 => DateSetting.Disabled.Format!,
                1 => DateSetting.Short.Format!,
                2 => DateSetting.Normal.Format!,
                3 => DateSetting.Long.Format!,
                _ => DateSetting.Short.Format!
            };
        }
    }
}
