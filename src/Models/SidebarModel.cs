using System;
using System.Diagnostics;

using System.ComponentModel;
using System.Windows.Threading;
using SSS.Core;
using SSS.Utilities;

namespace SSS.Models
{
    public class SidebarModel : INotifyPropertyChanged, IDisposable
    {
        public SidebarModel()
        {
            InitMachineName();
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
            PauseMonitors();
        }

        public void Resume()
        {
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

        private void InitMonitors()
        {
            MonitorManager = new MonitorManager(Core.Settings.Instance.Modules ?? ModuleDataConverter.GetDefaults());
            MonitorManager.Update();
        }

        private void StartMonitors()
        {
            _monitorTimer = new DispatcherTimer();
            _monitorTimer.Interval = TimeSpan.FromMilliseconds(Core.Settings.Instance.PollingInterval);
            _monitorTimer.Tick += new EventHandler(MonitorTimer_Tick);
            _monitorTimer.Start();

            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromMilliseconds(120);
            _clockTimer.Tick += new EventHandler(ClockTimer_Tick);
            _clockTimer.Start();
        }

        private void UpdateMonitors()
        {
            MonitorManager.Update();
        }

        public void TriggerHookUpdate(IntPtr frontHwnd)
        {
            MonitorManager?.UpdateFromHook(frontHwnd);
        }

        private void PauseMonitors()
        {
            _monitorTimer?.Stop();
            _clockTimer?.Stop();
        }

        private void ResumeMonitors()
        {
            _monitorTimer?.Start();
            _clockTimer?.Start();
        }

        private void DisposeMonitors()
        {
            _monitorTimer?.Stop();
            _monitorTimer = null;

            _clockTimer?.Stop();
            _clockTimer = null;

            if (MonitorManager != null)
            {
                MonitorManager.Dispose();
                _monitorManager = null;
            }
        }

        private void MonitorTimer_Tick(object? sender, EventArgs e)
        {
            UpdateMonitors();
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            MonitorManager?.UpdateTime();
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

        private DispatcherTimer? _monitorTimer { get; set; }

        private DispatcherTimer? _clockTimer { get; set; }

        private bool _disposed { get; set; } = false;
    }
}
