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
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width
        {
            get
            {
                return Right - Left;
            }
        }

        public int Height
        {
            get
            {
                return Bottom - Top;
            }
        }
    }

    public class WorkArea
    {
        public double Left { get; set; }

        public double Top { get; set; }

        public double Right { get; set; }

        public double Bottom { get; set; }

        public double Width
        {
            get
            {
                return Right - Left;
            }
        }

        public double Height
        {
            get
            {
                return Bottom - Top;
            }
        }

        public void Scale(double x, double y)
        {
            Left *= x;
            Top *= y;
            Right *= x;
            Bottom *= y;
        }

        public void Offset(double x, double y)
        {
            Left += x;
            Top += y;
            Right += x;
            Bottom += y;
        }

        public void SetWidth(DockEdge edge, double width)
        {
            switch (edge)
            {
                case DockEdge.Left:
                    Right = Left + width;
                    break;

                case DockEdge.Right:
                    Left = Right - width;
                    break;
            }
        }

        public static WorkArea FromRECT(RECT rect)
        {
            return new WorkArea()
            {
                Left = rect.Left,
                Top = rect.Top,
                Right = rect.Right,
                Bottom = rect.Bottom
            };
        }
    }

    public class Monitor
    {
        private const uint DPICONST = 96u;

        [StructLayout(LayoutKind.Sequential)]
        internal struct MONITORINFO
        {
            public int cbSize;
            public RECT Size;
            public RECT WorkArea;
            public uint _dwFlags;
            // 外側に見せるための bool 型のプロパティを生やす
            public bool IsPrimary
            {
                get => (_dwFlags & 1) != 0; // 1（MONITORINFOF_PRIMARY）なら true
                set => _dwFlags = value ? 1u : 0u;
            }
        }


        internal enum MONITOR_DPI_TYPE : int
        {
            MDT_EFFECTIVE_DPI = 0,
            MDT_ANGULAR_DPI = 1,
            MDT_RAW_DPI = 2,
            MDT_DEFAULT = MDT_EFFECTIVE_DPI
        }

        public RECT Size { get; set; }

        public RECT WorkArea { get; set; }

        public double DPIx { get; set; }

        public double ScaleX
        {
            get
            {
                return DPIx / DPICONST;
            }
        }

        public double InverseScaleX
        {
            get
            {
                return 1 / ScaleX;
            }
        }

        public double DPIy { get; set; }

        public double ScaleY
        {
            get
            {
                return DPIy / DPICONST;
            }
        }

        public double InverseScaleY
        {
            get
            {
                return 1 / ScaleY;
            }
        }

        public bool IsPrimary { get; set; }

        internal delegate bool EnumCallback(IntPtr hDesktop, IntPtr hdc, ref RECT pRect, int dwData);

        public static Monitor GetMonitor(IntPtr hMonitor)
        {
            MONITORINFO _info = new MONITORINFO();
            _info.cbSize = Marshal.SizeOf(_info);

            NativeMethods.GetMonitorInfo(hMonitor, ref _info);

            uint _dpiX = Monitor.DPICONST;
            uint _dpiY = Monitor.DPICONST;

            if (OS.SupportDPI)
            {
                NativeMethods.GetDpiForMonitor(hMonitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out _dpiX, out _dpiY);
            }

            return new Monitor()
            {
                Size = _info.Size,
                WorkArea = _info.WorkArea,
                DPIx = _dpiX,
                DPIy = _dpiY,
                IsPrimary = _info.IsPrimary
            };
        }

        public static Monitor[] GetMonitors()
        {
            List<Monitor> _monitors = new List<Monitor>();

            EnumCallback _callback = (IntPtr hMonitor, IntPtr hdc, ref RECT pRect, int dwData) =>
            {
                _monitors.Add(GetMonitor(hMonitor));

                return true;
            };

            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, _callback, 0);

            return _monitors.OrderByDescending(m => m.IsPrimary).ToArray();
        }

        public static Monitor GetMonitorFromIndex(int index)
        {
            return GetMonitorFromIndex(index, GetMonitors());
        }

        private static Monitor GetMonitorFromIndex(int index, Monitor[] monitors)
        {
            if (index < monitors.Length)
                return monitors[index];
            else
                return monitors.GetPrimary();
        }

        public static void GetWorkArea(AppBarWindow window, out int screen, out DockEdge edge, out WorkArea initPos, out WorkArea windowWA, out WorkArea appbarWA)
        {
            screen = Core.Settings.Instance.ScreenIndex;
            edge = Core.Settings.Instance.DockEdge;

            double _uiScale = Core.Settings.Instance.UIScale;

            if (OS.SupportDPI)
            {
                window.UpdateScale(_uiScale, _uiScale, false);
            }

            Monitor[] _monitors = GetMonitors();

            Monitor _primary = _monitors.GetPrimary();
            Monitor _active = GetMonitorFromIndex(screen, _monitors);

            initPos = new WorkArea()
            {
                Top = _active.WorkArea.Top,
                Left = _active.WorkArea.Left,
                Bottom = _active.WorkArea.Top + 10,
                Right = _active.WorkArea.Left + 10
            };

            windowWA = Windows.WorkArea.FromRECT(_active.WorkArea);
            
            double scaleX = _primary.ScaleX / _active.ScaleX;
            double scaleY = _primary.ScaleY / _active.ScaleY;
            windowWA.Scale(scaleX, scaleY);

            double _modifyX = 0d;
            double _modifyY = 0d;

            windowWA.Offset(_modifyX, _modifyY);

            double _windowWidth = Core.Settings.Instance.SidebarWidth * _uiScale;

            windowWA.SetWidth(edge, _windowWidth);

            int _offsetX = Core.Settings.Instance.XOffset;
            int _offsetY = Core.Settings.Instance.YOffset;

            windowWA.Offset(_offsetX, _offsetY);

            appbarWA = Windows.WorkArea.FromRECT(_active.WorkArea);

            appbarWA.Offset(_modifyX, _modifyY);

            double _appbarWidth = Core.Settings.Instance.UseAppBar ? windowWA.Width * _active.ScaleX : 0;

            appbarWA.SetWidth(edge, _appbarWidth);

            appbarWA.Offset(_offsetX, _offsetY);
        }
    }

    public static class MonitorExtensions
    {
        public static Monitor GetPrimary(this Monitor[] monitors)
        {
            return monitors.Where(m => m.IsPrimary).Single();
        }
    }

}
