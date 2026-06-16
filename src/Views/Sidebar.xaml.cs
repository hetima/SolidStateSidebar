using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SSS.Windows;
using SSS.Models;


namespace SSS
{
    /// <summary>
    /// Interaction logic for Sidebar.xaml
    /// </summary>
    public partial class Sidebar : AppBarWindow
    {
        public Sidebar(bool openSettings, bool initiallyHidden)
        {
            InitializeComponent();

#if DEBUG
            WindowControls.Margin = new Thickness(0, 32, 0, 0);
#endif

            _openSettings = openSettings;
            _initiallyHidden = initiallyHidden;
        }

        public void Reload()
        {
            if (!Ready)
            {
                return;
            }

            Ready = false;

            App._reloading = true;

            Close();
        }

        public async Task Reset(bool enableHotkeys)
        {
            if (!Ready)
            {
                return;
            }

            Ready = false;

            await BindSettings(enableHotkeys);

            await BindModel();
        }

        public async Task Reposition()
        {
            if (!Ready)
            {
                return;
            }

            Ready = false;

            await BindPosition();
            await BindModel();

            Ready = true;
        }

        public void ContentReload()
        {
            if (!Ready)
            {
                return;
            }

            Ready = false;

            Model?.Reload();

            Ready = true;
        }

        public override async Task AppBarShow()
        {
            await base.AppBarShow();

            Model?.Resume();
        }

        public override void AppBarHide()
        {
            base.AppBarHide();

            Model?.Pause();
        }

        private async Task Initialize()
        {
            Ready = false;

            Devices.AddHook(this);

            DisableAeroPeek();

            await BindSettings(true);

            await BindModel();
        }

        private async Task BindSettings(bool enableHotkeys)
        {
            await BindPosition();

            ShowDesktop.AddHook(this);

            if (Core.Settings.Instance.AlwaysTop)
            {
                SetTopMost(false);
            }
            else
            {
                ClearTopMost(false);
            }

            if (Core.Settings.Instance.ClickThrough)
            {
                SetClickThrough();
            }
            else
            {
                ClearClickThrough();
            }

            if (Core.Settings.Instance.ToolbarMode)
            {
                HideInAltTab();
            }
            else
            {
                ShowInAltTab();
            }

            if (WindowControls.Visibility != Visibility.Visible)
            {
                if (Core.Settings.Instance.CollapseMenuBar)
                {
                    WindowControls.Visibility = Visibility.Collapsed;
                }
                else
                {
                    WindowControls.Visibility = Visibility.Hidden;
                }
            }

            Hotkey.Initialize(this, Core.Settings.Instance.Hotkeys);

            if (enableHotkeys)
            {
                Hotkey.Enable();
            }
        }

        private async Task BindPosition()
        {
            await SetAppBar();
        }
        
        private async Task BindModel()
        {
            await Task.Run(async () =>
            {
                Model?.Dispose();
                Model = null;

                await Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ModelReadyHandler(ModelReady), new SidebarModel());
            });
        }

        private delegate void ModelReadyHandler(SidebarModel model);

        private void ModelReady(SidebarModel model)
        {
            DataContext = Model = model;
            model.Start();

            Ready = true;

            if (_openSettings)
            {
                _openSettings = false;

                App.Current.OpenSettings();
            }

            if (_initiallyHidden)
            {
                _initiallyHidden = false;

                AppBarHide();
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            App.Current.OpenSettings();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }
        
        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            WindowControls.Visibility = Visibility.Visible;
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            WindowControls.Visibility = Core.Settings.Instance.CollapseMenuBar ? Visibility.Collapsed : Visibility.Hidden;
        }

        private void ContentScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Model?.TryHandleWindowScrollSwitch(e.Delta) == true)
            {
                e.Handled = true;
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Initialize();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState != WindowState.Normal)
            {
                WindowState = WindowState.Normal;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Ready = false;

            DataContext = null;
            Model?.Dispose();
            Model = null;

            ClearAppBar();

            Devices.RemoveHook(this);
            ShowDesktop.RemoveHook();
            Hotkey.Dispose();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (App._reloading)
            {
                App._reloading = false;

                new Sidebar(false, false).Show();
            }
            else
            {
                App.Current.Shutdown();
            }
        }

        private bool _ready = false;

        public bool Ready
        {
            get
            {
                return _ready;
            }
            set
            {
                _ready = value;

                Model?.Ready = value;
            }
        }

        public SidebarModel? Model { get; private set; }

        public void TriggerMonitorUpdate(IntPtr frontHwnd)
        {
            Model?.TriggerHookUpdate(frontHwnd);
        }

        private bool _openSettings = false;

        private bool _initiallyHidden = false;
    }
}
