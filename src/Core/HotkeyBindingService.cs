using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using SidebarDiagnostics.Models;
using SidebarDiagnostics.Windows;

namespace SidebarDiagnostics.Core
{
    public class HotkeyBindingService
    {
        private Hotkey _hotkey;
        private ToggleButton _keybinder;
        private SettingsModel _model;
        private Window _window;

        public HotkeyBindingService(SettingsModel model, Window window)
        {
            _model = model;
            _window = window;
        }

        public void BeginBind(Hotkey.KeyAction action, ToggleButton keybinder)
        {
            _keybinder = keybinder;
            _hotkey = new Hotkey();
            _hotkey.Action = action;
            _hotkey.WinKey = Key.Escape;
            _window.KeyDown += Window_KeyDown;
        }

        public void EndBind()
        {
            _window.KeyDown -= Window_KeyDown;

            Hotkey.KeyAction _action = _hotkey.Action;

            if (_hotkey.WinKey == Key.Escape)
            {
                _hotkey = null;
            }

            switch (_action)
            {
                case Hotkey.KeyAction.Toggle:
                    _model.ToggleKey = _hotkey;
                    break;

                case Hotkey.KeyAction.Show:
                    _model.ShowKey = _hotkey;
                    break;

                case Hotkey.KeyAction.Hide:
                    _model.HideKey = _hotkey;
                    break;

                case Hotkey.KeyAction.Reload:
                    _model.ReloadKey = _hotkey;
                    break;

                case Hotkey.KeyAction.Close:
                    _model.CloseKey = _hotkey;
                    break;

                case Hotkey.KeyAction.CycleEdge:
                    _model.CycleEdgeKey = _hotkey;
                    break;

                case Hotkey.KeyAction.CycleScreen:
                    _model.CycleScreenKey = _hotkey;
                    break;

                case Hotkey.KeyAction.ReserveSpace:
                    _model.ReserveSpaceKey = _hotkey;
                    break;
            }

            if (_keybinder != null)
            {
                _keybinder.IsChecked = false;
            }
        }

        public void OnButtonLostFocus(object sender, RoutedEventArgs e)
        {
            if (_hotkey != null)
            {
                EndBind();
            }

            (sender as ToggleButton).IsChecked = false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            Key _key = e.Key == Key.System ? e.SystemKey : e.Key;

            if (new Key[] { Key.LeftAlt, Key.RightAlt, Key.LeftCtrl, Key.RightCtrl, Key.LeftShift, Key.RightShift, Key.LWin, Key.RWin }.Contains(_key))
            {
                return;
            }

            if ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                _hotkey.CtrlMod = true;
            }

            if ((e.KeyboardDevice.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                _hotkey.ShiftMod = true;
            }

            if ((e.KeyboardDevice.Modifiers & ModifierKeys.Windows) == ModifierKeys.Windows)
            {
                _hotkey.WinMod = true;
            }

            if ((e.KeyboardDevice.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                _hotkey.AltMod = true;
            }

            _hotkey.WinKey = _key;

            EndBind();

            e.Handled = true;
        }
    }
}