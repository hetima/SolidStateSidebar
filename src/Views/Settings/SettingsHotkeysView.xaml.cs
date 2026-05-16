using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using SidebarDiagnostics.Core;
using SidebarDiagnostics.Models;
using SidebarDiagnostics.Windows;

namespace SidebarDiagnostics
{
    /// <summary>
    /// Interaction logic for SettingsHotkeysView.xaml
    /// </summary>
    public partial class SettingsHotkeysView : UserControl
    {
        private HotkeyBindingService _hotkeyService;

        public SettingsHotkeysView()
        {
            InitializeComponent();

            Loaded += SettingsHotkeysView_Loaded;
        }

        private void SettingsHotkeysView_Loaded(object sender, RoutedEventArgs e)
        {
            // Get the SettingsModel from DataContext
            if (DataContext is SettingsModel model && Window.GetWindow(this) is Window window)
            {
                _hotkeyService = new HotkeyBindingService(model, window);
            }
        }

        private void BindButton_LostFocus(object sender, RoutedEventArgs e)
        {
            _hotkeyService?.OnButtonLostFocus(sender, e);
        }

        private void BindToggle_Click(object sender, RoutedEventArgs e)
        {
            var keybinder = (ToggleButton)sender;

            if (keybinder.IsChecked == true)
            {
                _hotkeyService?.BeginBind(Hotkey.KeyAction.Toggle, keybinder);
            }
            else
            {
                _hotkeyService?.EndBind();
            }
        }

        private void BindShow_Click(object sender, RoutedEventArgs e)
        {
            var keybinder = (ToggleButton)sender;

            if (keybinder.IsChecked == true)
            {
                _hotkeyService?.BeginBind(Hotkey.KeyAction.Show, keybinder);
            }
            else
            {
                _hotkeyService?.EndBind();
            }
        }

        private void BindHide_Click(object sender, RoutedEventArgs e)
        {
            var keybinder = (ToggleButton)sender;

            if (keybinder.IsChecked == true)
            {
                _hotkeyService?.BeginBind(Hotkey.KeyAction.Hide, keybinder);
            }
            else
            {
                _hotkeyService?.EndBind();
            }
        }

        private void BindReload_Click(object sender, RoutedEventArgs e)
        {
            var keybinder = (ToggleButton)sender;

            if (keybinder.IsChecked == true)
            {
                _hotkeyService?.BeginBind(Hotkey.KeyAction.Reload, keybinder);
            }
            else
            {
                _hotkeyService?.EndBind();
            }
        }

        private void BindClose_Click(object sender, RoutedEventArgs e)
        {
            var keybinder = (ToggleButton)sender;

            if (keybinder.IsChecked == true)
            {
                _hotkeyService?.BeginBind(Hotkey.KeyAction.Close, keybinder);
            }
            else
            {
                _hotkeyService?.EndBind();
            }
        }

        private void BindCycleEdge_Click(object sender, RoutedEventArgs e)
        {
            var keybinder = (ToggleButton)sender;

            if (keybinder.IsChecked == true)
            {
                _hotkeyService?.BeginBind(Hotkey.KeyAction.CycleEdge, keybinder);
            }
            else
            {
                _hotkeyService?.EndBind();
            }
        }

        private void BindCycleScreen_Click(object sender, RoutedEventArgs e)
        {
            var keybinder = (ToggleButton)sender;

            if (keybinder.IsChecked == true)
            {
                _hotkeyService?.BeginBind(Hotkey.KeyAction.CycleScreen, keybinder);
            }
            else
            {
                _hotkeyService?.EndBind();
            }
        }

        private void BindReserveSpace_Click(object sender, RoutedEventArgs e)
        {
            var keybinder = (ToggleButton)sender;

            if (keybinder.IsChecked == true)
            {
                _hotkeyService?.BeginBind(Hotkey.KeyAction.ReserveSpace, keybinder);
            }
            else
            {
                _hotkeyService?.EndBind();
            }
        }
    }
}