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
    public static class WindowHelper
    {
        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;
        private const byte VK_MENU = 0x12;
        private const uint KEYEVENTF_KEYUP = 0x0002u;

        // ★指定したハンドルを最前面に移動するメソッド
        public static void ActivateWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;

            // もしアイコン化（最小化）されていたら元に戻す
            NativeMethods.ShowWindow(hWnd, SW_RESTORE);

            // ウィンドウを最前面（アクティブ）にする
            NativeMethods.SetForegroundWindow(hWnd);
        }

        // public static void ActivateWindow(IntPtr hwnd)
        // {
        //     if (!NativeMethods.IsWindow(hwnd))
        //     {
        //         return;
        //     }

        //     bool isMinimized = NativeMethods.IsIconic(hwnd);

        //     if (isMinimized)
        //     {
        //         NativeMethods.ShowWindow(hwnd, SW_RESTORE);
        //     }

        //     uint targetThreadId = NativeMethods.GetWindowThreadProcessId(hwnd, out _);
        //     uint currentThreadId = NativeMethods.GetCurrentThreadId();

        //     if (targetThreadId != currentThreadId)
        //     {
        //         NativeMethods.AttachThreadInput(currentThreadId, targetThreadId, true);
        //     }

        //     if (!NativeMethods.SetForegroundWindow(hwnd))
        //     {
        //         // フォアグラウンドロック解除ハック
        //         NativeMethods.keybd_event(VK_MENU, 0, 0, UIntPtr.Zero);
        //         NativeMethods.keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        //         NativeMethods.SetForegroundWindow(hwnd);
        //     }

        //     if (targetThreadId != currentThreadId)
        //     {
        //         NativeMethods.AttachThreadInput(currentThreadId, targetThreadId, false);
        //     }

        //     NativeMethods.ShowWindow(hwnd, SW_SHOW);
        // }
    }
}
