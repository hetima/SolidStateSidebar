using System;
using System.Windows.Media;
using SVGImage.SVG;

namespace SSS.Core
{
    public class MonitorPanel : ObservableObject, IDisposable
    {
        public MonitorPanel(MonitorType type, string title, string? iconData, params iMonitor[] monitors)
        {
            Type = type;
            SvgContentPath = iconData;
            Title = title;

            Monitors = monitors;
        }

        private MonitorType _type;

        public MonitorType Type
        {
            get => _type;
            private set => SetProperty(ref _type, value);
        }

        public void Dispose()
        {
            Dispose(true);
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

        private string? _svgContentPath;

        public string? SvgContentPath
        {
            get => _svgContentPath;
            private set
            {
                _svgContentPath = value;
                NotifyPropertyChanged(nameof(SvgContentPath));
                NotifyPropertyChanged(nameof(SvgImageSource));
            }
        }

        private string? _title;

        public string Title
        {
            get => _title!;
            private set => SetProperty(ref _title, value);
        }

        private iMonitor[]? _monitors;

        public iMonitor[] Monitors
        {
            get => _monitors!;
            private set => SetProperty(ref _monitors, value);
        }

        private bool _disposed = false;

        private SectionHeaderStyle _sectionHeaderStyle = SectionHeaderStyle.Default;

        public SectionHeaderStyle SectionHeaderStyle
        {
            get => _sectionHeaderStyle;
            set => SetProperty(ref _sectionHeaderStyle, value);
        }

        public ResetTimeDisplay ShortResetDisplay { get; set; } = ResetTimeDisplay.Countdown;
        public ResetTimeDisplay LongResetDisplay  { get; set; } = ResetTimeDisplay.Countdown;
        public AutoRefreshInterval AutoRefresh    { get; set; } = AutoRefreshInterval.Manual;

        private int _fontSize = 0;

        public int FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }

        private string? _fontName = null;

        public string? FontName
        {
            get => _fontName;
            set => SetProperty(ref _fontName, value);
        }
    }
}
