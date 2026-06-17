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
    public static class Devices
    {
        private const int WM_DEVICECHANGE = 0x0219;

        private static class DBCH_DEVICETYPE
        {
            public const int DBT_DEVTYP_DEVICEINTERFACE = 5;
            public const int DBT_DEVTYP_HANDLE = 6;
            public const int DBT_DEVTYP_OEM = 0;
            public const int DBT_DEVTYP_PORT = 3;
            public const int DBT_DEVTYP_VOLUME = 2;
        }

        private static class FLAGS
        {
            public const int DEVICE_NOTIFY_WINDOW_HANDLE = 0;
            public const int DEVICE_NOTIFY_SERVICE_HANDLE = 1;
            public const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 4;
        }

        private static class WM_DEVICECHANGE_EVENT
        {
            public const int DBT_CONFIGCHANGECANCELED = 0x0019;
            public const int DBT_CONFIGCHANGED = 0x0018;
            public const int DBT_CUSTOMEVENT = 0x8006;
            public const int DBT_DEVICEARRIVAL = 0x8000;
            public const int DBT_DEVICEQUERYREMOVE = 0x8001;
            public const int DBT_DEVICEQUERYREMOVEFAILED = 0x8002;
            public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
            public const int DBT_DEVICEREMOVEPENDING = 0x8003;
            public const int DBT_DEVICETYPESPECIFIC = 0x8005;
            public const int DBT_DEVNODES_CHANGED = 0x0007;
            public const int DBT_QUERYCHANGECONFIG = 0x0017;
            public const int DBT_USERDEFINED = 0xFFFF;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DEV_BROADCAST_HDR
        {
            public int dbch_size;
            public int dbch_devicetype;
            public int dbch_reserved;
        }

        public static void AddHook(Sidebar window)
        {
            if (IsHooked)
            {
                return;
            }

            IsHooked = true;

            DEV_BROADCAST_HDR _data = new DEV_BROADCAST_HDR();
            _data.dbch_size = Marshal.SizeOf(_data);
            _data.dbch_devicetype = DBCH_DEVICETYPE.DBT_DEVTYP_DEVICEINTERFACE;

            IntPtr _buffer = Marshal.AllocHGlobal(_data.dbch_size);
            Marshal.StructureToPtr(_data, _buffer, true);

            IntPtr _hwnd = new WindowInteropHelper(window).Handle;

            NativeMethods.RegisterDeviceNotification(
                _hwnd,
                _buffer,
                FLAGS.DEVICE_NOTIFY_ALL_INTERFACE_CLASSES
                );

            window.HwndSource.AddHook(DeviceHook);
        }

        public static void RemoveHook(Sidebar window)
        {
            if (!IsHooked)
            {
                return;
            }

            IsHooked = false;

            window.HwndSource.RemoveHook(DeviceHook);
        }

        private static IntPtr DeviceHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_DEVICECHANGE)
            {
                switch (wParam.ToInt32())
                {
                    case WM_DEVICECHANGE_EVENT.DBT_DEVICEARRIVAL:
                    case WM_DEVICECHANGE_EVENT.DBT_DEVICEREMOVECOMPLETE:

                        _cancelRestart?.Cancel();

                        _cancelRestart = new CancellationTokenSource();

                        Task.Delay(TimeSpan.FromSeconds(1), _cancelRestart.Token).ContinueWith(_ =>
                        {
                            if (_.IsCanceled)
                            {
                                return;
                            }

                            App.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() =>
                            {
                                App.Current.Sidebar?.ContentReload();
                            }));

                            _cancelRestart = null;
                        });
                        break;
                }

                handled = true;
            }

            return IntPtr.Zero;
        }

        public static bool IsHooked { get; private set; } = false;

        private static CancellationTokenSource? _cancelRestart;
    }
}
