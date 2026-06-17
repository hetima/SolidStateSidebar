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
    [Serializable]
    public enum DockEdge : byte
    {
        Left,
        Top,
        Right,
        Bottom,
        None
    }

    public partial class AppBarWindow : DPIAwareWindow
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uCallbackMessage;
            public int uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        private static class APPBARMSG
        {
            public const int ABM_NEW = 0;
            public const int ABM_REMOVE = 1;
            public const int ABM_QUERYPOS = 2;
            public const int ABM_SETPOS = 3;
            public const int ABM_GETSTATE = 4;
            public const int ABM_GETTASKBARPOS = 5;
            public const int ABM_ACTIVATE = 6;
            public const int ABM_GETAUTOHIDEBAR = 7;
            public const int ABM_SETAUTOHIDEBAR = 8;
            public const int ABM_WINDOWPOSCHANGED = 9;
            public const int ABM_SETSTATE = 10;
        }

        private static class APPBARNOTIFY
        {
            public const int ABN_STATECHANGE = 0;
            public const int ABN_POSCHANGED = 1;
            public const int ABN_FULLSCREENAPP = 2;
            public const int ABN_WINDOWARRANGE = 3;
        }

        internal enum DWMWINDOWATTRIBUTE : int
        {
            DWMWA_NCRENDERING_ENABLED = 1,
            DWMWA_NCRENDERING_POLICY = 2,
            DWMWA_TRANSITIONS_FORCEDISABLED = 3,
            DWMWA_ALLOW_NCPAINT = 4,
            DWMWA_CAPTION_BUTTON_BOUNDS = 5,
            DWMWA_NONCLIENT_RTL_LAYOUT = 6,
            DWMWA_FORCE_ICONIC_REPRESENTATION = 7,
            DWMWA_FLIP3D_POLICY = 8,
            DWMWA_EXTENDED_FRAME_BOUNDS = 9,
            DWMWA_HAS_ICONIC_BITMAP = 10,
            DWMWA_DISALLOW_PEEK = 11,
            DWMWA_EXCLUDED_FROM_PEEK = 12,
            DWMWA_CLOAK = 13,
            DWMWA_CLOAKED = 14,
            DWMWA_FREEZE_REPRESENTATION = 15,
            DWMWA_LAST = 16
        }

        private static class HWND_FLAG
        {
            public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
            public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
            public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

            public const uint SWP_NOSIZE = 0x0001;
            public const uint SWP_NOMOVE = 0x0002;
            public const uint SWP_NOACTIVATE = 0x0010;
        }

        private static class WND_STYLE
        {
            public const int GWL_EXSTYLE = -20;

            public const long WS_EX_TRANSPARENT = 32;
            public const long WS_EX_TOOLWINDOW = 128;
        }

        private static class WM_WINDOWPOSCHANGING
        {
            public const int MSG = 0x0046;
            public const int SWP_NOMOVE = 0x0002;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WINDOWPOS
        {
            public IntPtr hWnd;
            public IntPtr hWndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            Loaded += AppBarWindow_Loaded;
        }

        private void AppBarWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PreventMove();
        }

        public void Move(WorkArea workArea)
        {
            AllowMove();

            Left = workArea.Left;
            Top = workArea.Top;
            Width = workArea.Width;
            Height = workArea.Height;

            PreventMove();
        }

        private void PreventMove()
        {
            if (!_canMove)
            {
                return;
            }

            _canMove = false;

            HwndSource.AddHook(MoveHook);
        }

        private void AllowMove()
        {
            if (_canMove)
            {
                return;
            }

            _canMove = true;

            HwndSource.RemoveHook(MoveHook);
        }

        private IntPtr MoveHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_WINDOWPOSCHANGING.MSG)
            {
#pragma warning disable CS8605
                WINDOWPOS _pos = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));
#pragma warning restore CS8605

                _pos.flags |= WM_WINDOWPOSCHANGING.SWP_NOMOVE;

                Marshal.StructureToPtr(_pos, lParam, true);

                handled = true;
            }

            return IntPtr.Zero;
        }

        public void SetTopMost(bool activate)
        {
            if (IsTopMost)
            {
                return;
            }

            IsTopMost = true;

            SetPos(HWND_FLAG.HWND_TOPMOST, activate);
        }

        public void ClearTopMost(bool activate)
        {
            if (!IsTopMost)
            {
                return;
            }

            IsTopMost = false;

            SetPos(HWND_FLAG.HWND_NOTOPMOST, activate);
        }

        public void SetBottom(bool activate)
        {
            IsTopMost = false;

            SetPos(HWND_FLAG.HWND_BOTTOM, activate);
        }

        private void SetPos(IntPtr hwnd_after, bool activate)
        {
            uint _uflags = HWND_FLAG.SWP_NOMOVE | HWND_FLAG.SWP_NOSIZE;

            if (!activate)
            {
                _uflags |= HWND_FLAG.SWP_NOACTIVATE;
            }

            NativeMethods.SetWindowPos(
                new WindowInteropHelper(this).Handle,
                hwnd_after,
                0,
                0,
                0,
                0,
                _uflags
                );
        }

        public void SetClickThrough()
        {
            if (IsClickThrough)
            {
                return;
            }

            IsClickThrough = true;

            SetWindowLong(WND_STYLE.WS_EX_TRANSPARENT, null);
        }

        public void ClearClickThrough()
        {
            if (!IsClickThrough)
            {
                return;
            }

            IsClickThrough = false;

            SetWindowLong(null, WND_STYLE.WS_EX_TRANSPARENT);
        }

        public void ShowInAltTab()
        {
            if (IsInAltTab)
            {
                return;
            }

            IsInAltTab = true;

            SetWindowLong(null, WND_STYLE.WS_EX_TOOLWINDOW);
        }

        public void HideInAltTab()
        {
            if (!IsInAltTab)
            {
                return;
            }

            IsInAltTab = false;

            SetWindowLong(WND_STYLE.WS_EX_TOOLWINDOW, null);
        }

        public void DisableAeroPeek()
        {
            IntPtr _hwnd = new WindowInteropHelper(this).Handle;

            IntPtr _status = Marshal.AllocHGlobal(sizeof(int));
            Marshal.WriteInt32(_status, 1);

            NativeMethods.DwmSetWindowAttribute(_hwnd, DWMWINDOWATTRIBUTE.DWMWA_EXCLUDED_FROM_PEEK, _status, sizeof(int));
        }

        private void SetWindowLong(long? add, long? remove)
        {
            IntPtr _hwnd = new WindowInteropHelper(this).Handle;

            long _style = NativeMethods.GetWindowLongPtr(_hwnd, WND_STYLE.GWL_EXSTYLE);

            if (add.HasValue)
            {
                _style |= add.Value;
            }

            if (remove.HasValue)
            {
                _style &= ~remove.Value;
            }

            NativeMethods.SetWindowLongPtr(_hwnd, WND_STYLE.GWL_EXSTYLE, _style);
        }

        public async Task SetAppBar()
        {
            ClearAppBar();

            await Task.Delay(100).ContinueWith(async (_) =>
            {
                await Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(async () =>
                {
                    await BindAppBar();
                }));
            });

        }

        private async Task BindAppBar()
        {
            Monitor.GetWorkArea(this, out int screen, out DockEdge edge, out WorkArea initPos, out WorkArea windowWA, out WorkArea appbarWA);

            Move(initPos);

            APPBARDATA _data = NewData();

            _callbackID = _data.uCallbackMessage = NativeMethods.RegisterWindowMessage("AppBarMessage");

            NativeMethods.SHAppBarMessage(APPBARMSG.ABM_NEW, ref _data);

            Screen = screen;
            DockEdge = edge;

            _data.uEdge = (int)edge;
            _data.rc = new RECT()
            {
                Left = (int)Math.Round(appbarWA.Left),
                Top = (int)Math.Round(appbarWA.Top),
                Right = (int)Math.Round(appbarWA.Right),
                Bottom = (int)Math.Round(appbarWA.Bottom)
            };

            NativeMethods.SHAppBarMessage(APPBARMSG.ABM_QUERYPOS, ref _data);

            NativeMethods.SHAppBarMessage(APPBARMSG.ABM_SETPOS, ref _data);

            IsAppBar = true;

            appbarWA.Left = _data.rc.Left;
            appbarWA.Top = _data.rc.Top;
            appbarWA.Right = _data.rc.Right;
            appbarWA.Bottom = _data.rc.Bottom;

            AppBarWidth = appbarWA.Width;

            await Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() =>
            {
                Move(windowWA);
            }));

            await Task.Delay(500).ContinueWith(async (_) =>
            {
                await Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() =>
                {
                    _monitorWorkArea = Monitor.GetMonitorFromIndex(Screen).WorkArea;
                    HwndSource.AddHook(AppBarHook);
                }));
            });
        }

        public void ClearAppBar()
        {
            if (!IsAppBar)
            {
                return;
            }

            HwndSource.RemoveHook(AppBarHook);

            APPBARDATA _data = NewData();

            NativeMethods.SHAppBarMessage(APPBARMSG.ABM_REMOVE, ref _data);

            IsAppBar = false;
        }

        public virtual async Task AppBarShow()
        {
            if (Core.Settings.Instance.UseAppBar)
            {
                await SetAppBar();
            }

            Show();
        }

        public virtual void AppBarHide()
        {
            Hide();

            if (IsAppBar)
            {
                ClearAppBar();
            }
        }

        private APPBARDATA NewData()
        {
            APPBARDATA _data = new APPBARDATA();
            _data.cbSize = Marshal.SizeOf(_data);
            _data.hWnd = new WindowInteropHelper(this).Handle;

            return _data;
        }

        private IntPtr AppBarHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == _callbackID)
            {
                switch (wParam.ToInt32())
                {
                    case APPBARNOTIFY.ABN_POSCHANGED:
                        //SetAppBar(); removed due to constant refreshing bug
                        break;

                    case APPBARNOTIFY.ABN_FULLSCREENAPP:
                        if (lParam.ToInt32() == 1)
                        {
                            _wasTopMost = IsTopMost;

                            if (IsTopMost)
                            {
                                SetBottom(false);
                            }
                        }
                        else if (_wasTopMost)
                        {
                            SetTopMost(false);
                        }
                        break;
                }

                handled = true;
            }

            return IntPtr.Zero;
        }

        public bool IsTopMost { get; private set; } = false;

        public bool IsClickThrough { get; private set; } = false;

        public bool IsInAltTab { get; private set; } = true;

        public bool IsAppBar { get; private set; } = false;

        public int Screen { get; private set; } = 0;

        public DockEdge DockEdge { get; private set; } = DockEdge.None;

        public double AppBarWidth { get; private set; } = 0;

        private bool _canMove = true;

        private bool _wasTopMost = false;

        private int _callbackID;

        private RECT _monitorWorkArea;
    }
}
