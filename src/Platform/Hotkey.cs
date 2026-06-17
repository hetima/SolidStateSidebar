using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Media;
using Newtonsoft.Json;

namespace SSS.Windows
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Hotkey
    {
        private const int WM_HOTKEY = 0x0312;

        private static class MODIFIERS
        {
            public const uint MOD_NOREPEAT = 0x4000;
            public const uint MOD_ALT = 0x0001;
            public const uint MOD_CONTROL = 0x0002;
            public const uint MOD_SHIFT = 0x0004;
            public const uint MOD_WIN = 0x0008;
        }

        public enum KeyAction : byte
        {
            Toggle,
            Show,
            Hide,
            Reload,
            Close,
            CycleEdge,
            CycleScreen,
            ReserveSpace
        }

        public Hotkey() { }

        public Hotkey(int index, KeyAction action, uint virtualKey, bool altMod = false, bool ctrlMod = false, bool shiftMod = false, bool winMod = false)
        {
            Index = index;
            Action = action;
            VirtualKey = virtualKey;
            AltMod = altMod;
            CtrlMod = ctrlMod;
            ShiftMod = shiftMod;
            WinMod = winMod;
        }

        [JsonProperty]
        public KeyAction Action { get; set; }

        [JsonProperty]
        public uint VirtualKey { get; set; }

        [JsonProperty]
        public bool AltMod { get; set; }

        [JsonProperty]
        public bool CtrlMod { get; set; }

        [JsonProperty]
        public bool ShiftMod { get; set; }

        [JsonProperty]
        public bool WinMod { get; set; }

        public Key WinKey
        {
            get
            {
                return KeyInterop.KeyFromVirtualKey((int)VirtualKey);
            }
            set
            {
                VirtualKey = (uint)KeyInterop.VirtualKeyFromKey(value);
            }
        }

        private int Index { get; set; }

        public static void Initialize(Sidebar window, Hotkey[] settings)
        {
            if (settings == null || settings.Length == 0)
            {
                Dispose();
                return;
            }

            Disable();

            _sidebar = window;
            _index = 0;

            RegisteredKeys = settings.Select(h =>
            {
                h.Index = _index;
                _index++;
                return h;
            }).ToArray();

            window.HwndSource.AddHook(KeyHook);

            IsHooked = true;
        }

        public static void Dispose()
        {
            if (!IsHooked)
            {
                return;
            }

            IsHooked = false;

            Disable();

            RegisteredKeys = null;

            _sidebar?.HwndSource.RemoveHook(KeyHook);
            _sidebar = null;
        }

        public static void Enable()
        {
            if (RegisteredKeys == null)
            {
                return;
            }

            foreach (Hotkey _hotkey in RegisteredKeys)
            {
                Register(_hotkey);
            }
        }

        public static void Disable()
        {
            if (RegisteredKeys == null)
            {
                return;
            }

            foreach (Hotkey _hotkey in RegisteredKeys)
            {
                Unregister(_hotkey);
            }
        }

        private static void Register(Hotkey hotkey)
        {
            if (_sidebar == null)
            {
                return;
            }

            uint _mods = MODIFIERS.MOD_NOREPEAT;

            if (hotkey.AltMod)
            {
                _mods |= MODIFIERS.MOD_ALT;
            }

            if (hotkey.CtrlMod)
            {
                _mods |= MODIFIERS.MOD_CONTROL;
            }

            if (hotkey.ShiftMod)
            {
                _mods |= MODIFIERS.MOD_SHIFT;
            }

            if (hotkey.WinMod)
            {
                _mods |= MODIFIERS.MOD_WIN;
            }

            NativeMethods.RegisterHotKey(
                new WindowInteropHelper(_sidebar).Handle,
                hotkey.Index,
                _mods,
                hotkey.VirtualKey
                );
        }

        private static void Unregister(Hotkey hotkey)
        {
            if (_sidebar == null)
            {
                return;
            }

            NativeMethods.UnregisterHotKey(
                new WindowInteropHelper(_sidebar).Handle,
                hotkey.Index
                );
        }

        public static Hotkey[]? RegisteredKeys { get; private set; }

        private static IntPtr KeyHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int _id = wParam.ToInt32();

                Hotkey? _hotkey = RegisteredKeys?.FirstOrDefault(k => k.Index == _id);

                if (_hotkey != null && _sidebar != null && _sidebar.Ready)
                {
                    switch (_hotkey.Action)
                    {
                        case KeyAction.Toggle:
                            if (_sidebar.Visibility == Visibility.Visible)
                            {
                                _sidebar.AppBarHide();
                            }
                            else
                            {
                                _ = _sidebar.AppBarShow();
                            }
                            break;

                        case KeyAction.Show:
                            _ = _sidebar.AppBarShow();
                            break;

                        case KeyAction.Hide:
                            _sidebar.AppBarHide();
                            break;

                        case KeyAction.Reload:
                            _sidebar.Reload();
                            break;

                        case KeyAction.Close:
                            App.Current.Shutdown();
                            break;

                        case KeyAction.CycleEdge:
                            if (_sidebar.Visibility == Visibility.Visible)
                            {
                                switch (Core.Settings.Instance.DockEdge)
                                {
                                    case DockEdge.Right:
                                        Core.Settings.Instance.DockEdge = DockEdge.Left;
                                        break;

                                    default:
                                    case DockEdge.Left:
                                        Core.Settings.Instance.DockEdge = DockEdge.Right;
                                        break;
                                }

                                Core.Settings.Instance.Save();

                                _ = _sidebar.Reposition();
                            }
                            break;

                        case KeyAction.CycleScreen:
                            if (_sidebar.Visibility == Visibility.Visible)
                            {
                                Monitor[] _monitors = Monitor.GetMonitors();

                                if (Core.Settings.Instance.ScreenIndex < (_monitors.Length - 1))
                                {
                                    Core.Settings.Instance.ScreenIndex++;
                                }
                                else
                                {
                                    Core.Settings.Instance.ScreenIndex = 0;
                                }

                                Core.Settings.Instance.Save();

                                _ = _sidebar.Reposition();
                            }
                            break;

                        case KeyAction.ReserveSpace:
                            Core.Settings.Instance.UseAppBar = !Core.Settings.Instance.UseAppBar;
                            Core.Settings.Instance.Save();

                            _ = _sidebar.Reposition();
                            break;
                    }

                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        public static bool IsHooked { get; private set; } = false;

        private static Sidebar? _sidebar;

        private static int _index;
    }
}
