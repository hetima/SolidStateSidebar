using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SSS.Utilities;

namespace SSS.Core
{
    public class ClockTimeMetric : BaseMetric
    {
        private readonly bool _clock24HR;

        public ClockTimeMetric(bool clock24HR) : base(MetricKey.Time, DataType.Dynamic, "")
        {
            _clock24HR = clock24HR;
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

        public ClockDateMetric(string format) : base(MetricKey.Date, DataType.Dynamic, "")
        {
            _format = format;
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

            Text = _now.ToString(_format, Culture.Default);
        }
    }

    public class ClockMonitor : BaseMonitor
    {
        public ClockMonitor(MetricConfig[] metrics, bool clock24HR, int dateFormatValue) : base("clock", Strings.Time, false)
        {
            List<iMetric> _metrics = [];

            if (metrics.IsEnabled(MetricKey.Date))
            {
                string _format = GetDateFormat(dateFormatValue);

                _metrics.Add(new ClockDateMetric(_format));
            }

            if (metrics.IsEnabled(MetricKey.Time))
            {
                _metrics.Add(new ClockTimeMetric(clock24HR));
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

        public static iMonitor[] GetInstances(HardwareConfig[] hardwareConfig, MetricConfig[] metrics, bool clock24HR, int dateFormatValue)
        {
            return
            [
                new ClockMonitor(metrics, clock24HR, dateFormatValue)
            ];
        }

        private static string GetDateFormat(int value)
        {
            return value switch
            {
                1 => DateSetting.Short.Format!,
                2 => DateSetting.Normal.Format!,
                3 => DateSetting.Long.Format!,
                _ => DateSetting.Short.Format!
            };
        }
    }
}
