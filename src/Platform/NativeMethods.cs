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
    internal static partial class NativeMethods
    {
        [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial long GetWindowLongPtr(IntPtr hwnd, int index);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial long SetWindowLongPtr(IntPtr hwnd, int index, long newStyle);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetWindowPos(IntPtr hwnd, IntPtr hwnd_after, int x, int y, int cx, int cy, uint uflags);

        [LibraryImport("user32.dll")]
        internal static partial IntPtr GetForegroundWindow();

        [LibraryImport("user32.dll")]
        internal static partial IntPtr GetWindow(IntPtr hwnd, uint uCmd);

        [LibraryImport("user32.dll")]
        internal static partial IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [LibraryImport("user32.dll", EntryPoint = "RegisterWindowMessageW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int RegisterWindowMessage(string msg);

        [LibraryImport("shell32.dll")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })]
        internal static partial UIntPtr SHAppBarMessage(int dwMessage, ref AppBarWindow.APPBARDATA pData);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EnumDisplayMonitors(IntPtr hdc, IntPtr lpRect, Monitor.EnumCallback callback, int dwData);

        [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetMonitorInfo(IntPtr hMonitor, ref Monitor.MONITORINFO lpmi);

        [LibraryImport("shcore.dll")]
        internal static partial IntPtr GetDpiForMonitor(IntPtr hmonitor, Monitor.MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY);

        [LibraryImport("user32.dll")]
        internal static partial IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint vk);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool UnregisterHotKey(IntPtr hwnd, int id);

        [LibraryImport("user32.dll", EntryPoint = "RegisterDeviceNotificationW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial IntPtr RegisterDeviceNotification(IntPtr recipient, IntPtr notificationFilter, int flags);

        //[LibraryImport("user32.dll")]
        //internal static partial bool UnregisterDeviceNotification(IntPtr handle);

        [LibraryImport("user32.dll")]
        internal static partial IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, ShowDesktop.WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool UnhookWinEvent(IntPtr hWinEventHook);

        [LibraryImport("user32.dll", EntryPoint = "GetClassNameW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int GetClassName(IntPtr hwnd, IntPtr name, int count);

        [LibraryImport("dwmapi.dll")]
        internal static partial int DwmSetWindowAttribute(IntPtr hwnd, AppBarWindow.DWMWINDOWATTRIBUTE dwmAttribute, IntPtr pvAttribute, uint cbAttribute);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int GetWindowText(IntPtr hwnd, IntPtr lpString, int nMaxCount);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowTextLengthW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int GetWindowTextLength(IntPtr hwnd);

        [LibraryImport("user32.dll")]
        internal static partial uint GetWindowThreadProcessId(IntPtr hwnd, out uint lpdwProcessId);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool IsWindowVisible(IntPtr hwnd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool IsIconic(IntPtr hwnd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool IsWindow(IntPtr hwnd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool ShowWindow(IntPtr hwnd, int nCmdShow);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetForegroundWindow(IntPtr hwnd);

        // [LibraryImport("user32.dll")]
        // internal static partial uint GetCurrentThreadId();

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

        [LibraryImport("user32.dll")]
        internal static partial void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [LibraryImport("user32.dll", EntryPoint = "PostMessageW", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        // ★ アイコン取得に必要な2つのAPI
        [LibraryImport("user32.dll", EntryPoint = "SendMessageW")] // 明示的にUnicode版を指定
        internal static partial IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        // ※GetClassLongPtrは64bit環境に対応するため、EntrypointでGetClassLongPtrを指定（32bit時は内部で自動フォールバックされます）
        [LibraryImport("user32.dll", EntryPoint = "GetClassLongPtrW")]
        internal static partial IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool DestroyIcon(IntPtr hIcon);

        internal delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
    }
}
