using System;
using System.ComponentModel;
using System.Windows.Media;
using SVGImage.SVG;

namespace SSS.Core
{
    public class MonitorPanel : INotifyPropertyChanged, IDisposable
    {
        public MonitorPanel(MonitorType type, string title, string? iconData, params iMonitor[] monitors)
        {
            Type = type;
            SvgContentPath = iconData;
            Title = title;

            Monitors = monitors;
        }

        private MonitorType _type { get; set; }

        public MonitorType Type
        {
            get
            {
                return _type;
            }
            private set
            {
                _type = value;
                NotifyPropertyChanged(nameof(Type));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (iMonitor _monitor in Monitors)
                    {
                        _monitor.Dispose();
                    }

                    _monitors = null;
                    _svgContentPath = null;
                }

                _disposed = true;
            }
        }

        ~MonitorPanel()
        {
            Dispose(false);
        }

        public ImageSource? SvgImageSource
        {
            get
            {
                if (string.IsNullOrEmpty(_svgContentPath)) return null;

                var render = new SVGRender();
                Color clr = Core.Settings.Instance.FontColorColor;
                render.OverrideColor = clr;
                render.OverrideFillColor = clr;
                DrawingGroup drawing = render.LoadDrawing(_svgContentPath);
                if (drawing == null) return null;
                return new DrawingImage(drawing);
            }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private String? _svgContentPath { get; set; }

        public String? SvgContentPath
        {
            get
            {
                return _svgContentPath;
            }
            private set
            {
                _svgContentPath = value;
                NotifyPropertyChanged(nameof(SvgContentPath));
                NotifyPropertyChanged(nameof(SvgImageSource));
            }
        }

        private string? _title { get; set; }

        public string Title
        {
            get
            {
                return _title!;
            }
            private set
            {
                _title = value;

                NotifyPropertyChanged(nameof(Title));
            }
        }

        private iMonitor[]? _monitors { get; set; }

        public iMonitor[] Monitors
        {
            get
            {
                return _monitors!;
            }
            private set
            {
                _monitors = value;

                NotifyPropertyChanged(nameof(Monitors));
            }
        }

        private bool _disposed { get; set; } = false;

        private SectionHeaderStyle _sectionHeaderStyle { get; set; } = SectionHeaderStyle.Default;

        public SectionHeaderStyle SectionHeaderStyle
        {
            get
            {
                return _sectionHeaderStyle;
            }
            set
            {
                _sectionHeaderStyle = value;
                NotifyPropertyChanged(nameof(SectionHeaderStyle));
            }
        }

        public ResetTimeDisplay ShortResetDisplay { get; set; } = ResetTimeDisplay.Countdown;
        public ResetTimeDisplay LongResetDisplay  { get; set; } = ResetTimeDisplay.Countdown;
        public AutoRefreshInterval AutoRefresh    { get; set; } = AutoRefreshInterval.Manual;

        private int _fontSize = 0;

        public int FontSize
        {
            get
            {
                return _fontSize;
            }
            set
            {
                _fontSize = value;
                NotifyPropertyChanged(nameof(FontSize));
            }
        }

        private string? _fontName = null;

        public string? FontName
        {
            get
            {
                return _fontName;
            }
            set
            {
                _fontName = value;
                NotifyPropertyChanged(nameof(FontName));
            }
        }
    }
}
