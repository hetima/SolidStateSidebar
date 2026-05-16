using System;
using System.ComponentModel;
using System.Windows.Threading;
using SSS.Core;
using SSS.Utilities;
using System.Diagnostics;
using System.Windows.Media;

namespace SSS.Models
{
    public class SidebarModel : INotifyPropertyChanged, IDisposable
    {
        public SidebarModel()
        {
            InitMachineName();
            InitClock();
            InitMonitors();
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
                    DisposeClock();
                    DisposeMonitors();
                }

                _disposed = true;
            }
        }

        ~SidebarModel()
        {
            Dispose(false);
        }

        public void Start()
        {
            StartClock();
            StartMonitors();
        }

        public void Reload()
        {
            DisposeMonitors();
            InitMonitors();
            StartMonitors();
        }

        public void Pause()
        {
            PauseClock();
            PauseMonitors();
        }

        public void Resume()
        {
            ResumeClock();
            ResumeMonitors();
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void InitMachineName()
        {
            ShowMachineName = Core.Settings.Instance.ShowMachineName;

            MachineName = Environment.MachineName;
        }

        private void InitClock()
        {
            ShowClock = Core.Settings.Instance.ShowClock;

            if (!ShowClock)
            {
                return;
            }

            ShowDate = !Core.Settings.Instance.DateSetting.Equals(Core.DateSetting.Disabled);

            UpdateClock();
        }

        private void InitMonitors()
        {
            MonitorManager = new MonitorManager(Core.Settings.Instance.MonitorConfig ?? []);
            MonitorManager.Update();
        }

        private void StartClock()
        {
            if (!ShowClock)
            {
                return;
            }

            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += new EventHandler(ClockTimer_Tick);
            _clockTimer.Start();
        }

        private void StartMonitors()
        {
            _monitorTimer = new DispatcherTimer();
            _monitorTimer.Interval = TimeSpan.FromMilliseconds(Core.Settings.Instance.PollingInterval);
            _monitorTimer.Tick += new EventHandler(MonitorTimer_Tick);
            _monitorTimer.Start();
        }

        private void UpdateClock()
        {
            DateTime _now = DateTime.Now;

            Time = _now.ToString(Core.Settings.Instance.Clock24HR ? "H:mm:ss" : "h:mm:ss tt", Culture.Default);

            if (ShowDate)
            {
                Date = _now.ToString(Core.Settings.Instance.DateSetting.Format, Culture.Default);
            }
        }

        private void UpdateMonitors()
        {
            MonitorManager.Update();
        }

        private void PauseClock()
        {
            _clockTimer?.Stop();
        }

        private void PauseMonitors()
        {
            _monitorTimer?.Stop();
        }

        private void ResumeClock()
        {
            _clockTimer?.Start();
        }

        private void ResumeMonitors()
        {
            _monitorTimer?.Start();
        }

        private void DisposeClock()
        {
            _clockTimer?.Stop();
            _clockTimer = null;
        }

        private void DisposeMonitors()
        {
            _monitorTimer?.Stop();
            _monitorTimer = null;

            if (MonitorManager != null)
            {
                MonitorManager.Dispose();
                _monitorManager = null;
            }
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            UpdateClock();
        }

        private void MonitorTimer_Tick(object? sender, EventArgs e)
        {
            UpdateMonitors();
        }

        private bool _ready { get; set; } = false;

        public bool Ready
        {
            get
            {
                return _ready;
            }
            set
            {
                _ready = value;

                NotifyPropertyChanged(nameof(Ready));
            }
        }

        private bool _showMachineName { get; set; }

        public bool ShowMachineName
        {
            get
            {
                return _showMachineName;
            }
            set
            {
                _showMachineName = value;

                NotifyPropertyChanged(nameof(ShowMachineName));
            }
        }

        private string? _machineName { get; set; }

        public string MachineName
        {
            get
            {
                return _machineName ?? string.Empty;
            }
            set
            {
                _machineName = value;

                NotifyPropertyChanged(nameof(MachineName));
            }
        }

        private bool _showClock { get; set; }

        public bool ShowClock
        {
            get
            {
                return _showClock;
            }
            set
            {
                _showClock = value;

                NotifyPropertyChanged(nameof(ShowClock));
            }
        }
        public static ImageSource? ClockSvg
        {
            get
            {
                string? svgContentPath = Core.Settings.Instance.GetIconSvgPath("clock");
                if (string.IsNullOrEmpty(svgContentPath)) return null;

                var render = new SVGImage.SVG.SVGRender();
                Color clr = (Color)ColorConverter.ConvertFromString(Core.Settings.Instance.FontColor);
                render.OverrideColor = clr;
                render.OverrideFillColor = clr;
                DrawingGroup drawing = render.LoadDrawing(svgContentPath);
                if (drawing == null) return null;
                return new DrawingImage(drawing);
            }
        }

        private string? _time { get; set; }

        public string Time
        {
            get
            {
                return _time ?? string.Empty;
            }
            set
            {
                _time = value;

                NotifyPropertyChanged(nameof(Time));
            }
        }

        private bool _showDate { get; set; }

        public bool ShowDate
        {
            get
            {
                return _showDate;
            }
            set
            {
                _showDate = value;

                NotifyPropertyChanged(nameof(ShowDate));
            }
        }

        private string? _date { get; set; }

        public string Date
        {
            get
            {
                return _date ?? string.Empty;
            }
            set
            {
                _date = value;

                NotifyPropertyChanged(nameof(Date));
            }
        }

        private MonitorManager? _monitorManager { get; set; }

        public MonitorManager MonitorManager
        {
            get
            {
                return _monitorManager!;
            }
            set
            {
                _monitorManager = value;

                NotifyPropertyChanged(nameof(MonitorManager));
            }
        }

        private DispatcherTimer? _clockTimer { get; set; }

        private DispatcherTimer? _monitorTimer { get; set; }

        private bool _disposed { get; set; } = false;
    }
}
