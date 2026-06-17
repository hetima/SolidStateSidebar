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
    public static class ShowDesktop
    {
        private const uint WINEVENT_OUTOFCONTEXT = 0u;
        private const uint EVENT_SYSTEM_FOREGROUND = 3u;

        private const string WORKERW = "WorkerW";
        private const string PROGMAN = "Progman";

        public static void AddHook(Sidebar sidebar)
        {
            if (IsHooked)
            {
                return;
            }

            IsHooked = true;

            _delegate = new WinEventDelegate(WinEventHook);
            _hookIntPtr = NativeMethods.SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _delegate, 0, 0, WINEVENT_OUTOFCONTEXT);
            _sidebar = sidebar;
            _sidebarHwnd = new WindowInteropHelper(sidebar).Handle;
        }

        public static void RemoveHook()
        {
            if (!IsHooked)
            {
                return;
            }

            IsHooked = false;

            if (_hookIntPtr != null)
            {
                NativeMethods.UnhookWinEvent(_hookIntPtr.Value);
            }

            _delegate = null;
            _hookIntPtr = null;
            _sidebar = null;
            _sidebarHwnd = null;
        }

        public static string GetWindowClass(IntPtr hwnd)
        {
            Span<char> buffer = stackalloc char[260];

            unsafe
            {
                fixed (char* pBuffer = buffer)
                {
                    int length = NativeMethods.GetClassName(hwnd, (IntPtr)pBuffer, buffer.Length);
                    if (length > 0)
                    {
                        // 実際に書き込まれた長さ分だけを切り出して文字列化
                        return buffer[..length].ToString();
                    }
                }
            }
            return string.Empty;
        }

        internal delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        private static void WinEventHook(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType == EVENT_SYSTEM_FOREGROUND)
            {
                // AlwaysTop でない場合のみ Z-order 操作を行う
                if (!Core.Settings.Instance.AlwaysTop)
                {
                    string _class = GetWindowClass(hwnd);

                    if (string.Equals(_class, WORKERW, StringComparison.Ordinal))
                    {
                        _sidebar?.SetTopMost(false);
                    }
                    else if (_sidebar != null && _sidebar.IsTopMost)
                    {
                        _sidebar.ClearTopMost(false);
                    }
                }

                // フォアグラウンドウィンドウ変更時にモニター更新をトリガー
                // Z-order の反映を待つため Dispatcher で遅延実行
                var hwndCapture = hwnd;
                _sidebar?.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (Action)(() =>
                {
                    _sidebar?.TriggerMonitorUpdate(hwndCapture);
                }));
            }
        }

        public static bool IsHooked { get; private set; } = false;

        private static IntPtr? _hookIntPtr;

        private static WinEventDelegate? _delegate;

        private static Sidebar? _sidebar;

        private static IntPtr? _sidebarHwnd;
    }
}
