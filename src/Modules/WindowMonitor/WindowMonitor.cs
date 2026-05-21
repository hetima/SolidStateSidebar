using System;
using System.ComponentModel;
using SSS.Core;
using SSS.Utilities;

namespace SSS.Module.WindowMonitor
{
    public class WindowTitleMetric : BaseMetric
    {
        private int _fontSize;

        public WindowTitleMetric(int fontSize) : base(MetricKey.WindowTitle, DataType.Dynamic, "Window")
        {
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

        ~WindowTitleMetric()
        {
            Dispose(false);
        }

        public override bool IsNumeric
        {
            get { return false; }
        }

        public override void Update()
        {
            // TODO: アクティブウィンドウのタイトルを取得
            Text = "";
        }
    }

    public class WindowMonitor : BaseMonitor
    {
        public WindowMonitor(int fontSize) : base("window", "Window", false)
        {
            Metrics =
            [
                new WindowTitleMetric(fontSize)
            ];
        }

        ~WindowMonitor()
        {
            Dispose(false);
        }

        public static iMonitor[] GetInstances(int fontSize = 12)
        {
            return
            [
                new WindowMonitor(fontSize)
            ];
        }
    }
}
