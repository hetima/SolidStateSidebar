using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Hardcodet.Wpf.TaskbarNotification;
using SSS;
using SSS.Core;
using SSS.Utilities;
using SSS.Windows;

namespace SSS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string[]? FontNameItems { get; private set; }

        public static string[] InitFontNameItems()
        {
            FontNameItems ??= Fonts.SystemFontFamilies.Select(i => i.Source).ToArray();
            if(FontNameItems == null || FontNameItems.Length == 0)
            {
                FontNameItems = ["Segoe UI"];
            }
            return FontNameItems;
        }

        protected async override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // THEME
#pragma warning disable WPF0001
            ThemeMode = ThemeMode.Light;
#pragma warning restore WPF0001

            // ERROR HANDLING
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(AppDomain_Error);
#endif

            // LANGUAGE
            Culture.SetDefault();
            Culture.SetCurrent(true);


            // SETTINGS
            CheckSettings();

            // VERSION
            Version? _version = Assembly.GetExecutingAssembly().GetName().Version;
            string _vstring = _version?.ToString(3) ?? "0.0.0";

            // TRAY ICON
            TrayIcon = FindResource("TrayIcon") as TaskbarIcon;
            if (TrayIcon != null)
            {
                TrayIcon.ToolTipText = string.Format("{0} v{1}", Strings.AppName, _vstring);
                TrayIcon.TrayContextMenuOpen += TrayIcon_TrayContextMenuOpen;
            }

            // START APP
            if (Core.Settings.Instance.InitialSetup)
            {
                new Setup();
            }
            else
            {
                StartApp(false);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            TrayIcon?.Dispose();

            base.OnExit(e);
        }

        public static void StartApp(bool openSettings)
        {
            new Sidebar(openSettings, Core.Settings.Instance.InitiallyHidden).Show();

            RefreshIcon();
        }

        public static void RefreshIcon()
        {
            if (TrayIcon != null)
            {
                TrayIcon.Visibility = Core.Settings.Instance.ShowTrayIcon ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public void OpenSettings()
        {
            Settings? _settings = Windows.OfType<Settings>().FirstOrDefault();

            if (_settings != null)
            {
                _settings.WindowState = System.Windows.WindowState.Normal;
                _settings.Activate();
                return;
            }

            Sidebar? _sidebar = Sidebar;

            if (_sidebar == null)
            {
                return;
            }
            _ = new Settings(_sidebar);
        }

        private void CheckSettings()
        {
            if (Core.Settings.Instance.RunAtStartup)
            {
                Utilities.Startup.EnableStartupTask();
            }

            Core.Settings.Instance.Modules = Core.Settings.CheckModules(Core.Settings.Instance.Modules);
        }

        private void TrayIcon_TrayContextMenuOpen(object sender, RoutedEventArgs e)
        {
            var tray = TrayIcon;
            var cm = tray?.ContextMenu;
            if (cm == null) return;

            Monitor _primary = Monitor.GetMonitors().GetPrimary();

            cm.HorizontalOffset *= _primary.InverseScaleX;
            cm.VerticalOffset *= _primary.InverseScaleY;
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            OpenSettings();
        }

        private void Reload_Click(object sender, EventArgs e)
        {
            Sidebar? _sidebar = Sidebar;

            if (_sidebar == null)
            {
                return;
            }

            _sidebar.Reload();
        }

        private void Visibility_SubmenuOpened(object sender, EventArgs e)
        {
            Sidebar? _sidebar = Sidebar;

            if (_sidebar == null)
            {
                return;
            }

            if (!(sender is MenuItem _this))
            {
                return;
            }

            var item0 = _this.Items.GetItemAt(0) as MenuItem;
            if (item0 != null) item0.IsChecked = _sidebar.Visibility == Visibility.Visible;

            var item1 = _this.Items.GetItemAt(1) as MenuItem;
            if (item1 != null) item1.IsChecked = _sidebar.Visibility == Visibility.Hidden;
        }
        
        private void Show_Click(object sender, EventArgs e)
        {
            Sidebar? _sidebar = Sidebar;

            if (_sidebar == null || _sidebar.Visibility == Visibility.Visible)
            {
                return;
            }

            _sidebar?.AppBarShow();
        }

        private void Hide_Click(object sender, EventArgs e)
        {
            Sidebar? _sidebar = Sidebar;

            if (_sidebar == null || _sidebar.Visibility == Visibility.Hidden)
            {
                return;
            }

            _sidebar.AppBarHide();
        }

        private void GitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/hetima/SolidStateSidebar") { UseShellExecute = true });
        }


        private void Close_Click(object sender, EventArgs e)
        {
            Shutdown();
        }
        
        private static void AppDomain_Error(object sender, UnhandledExceptionEventArgs e)
        {
            Exception? ex = e.ExceptionObject as Exception;
            string text = ex?.ToString() ?? e.ExceptionObject?.ToString() ?? "Unknown error";

            System.Windows.MessageBox.Show(text, Strings.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }
        
        public Sidebar? Sidebar
        {
            get
            {
                return Windows.OfType<Sidebar>().FirstOrDefault();
            }
        }

        public new static App Current
        {
            get
            {
                return Application.Current as App ?? throw new InvalidOperationException("Application.Current is null");
            }
        }

        public static TaskbarIcon? TrayIcon { get; set; }

        internal static bool _reloading { get; set; } = false;
    }
}