using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using System.Windows;
using SSS.Core;
using SSS.Windows;

namespace SSS.Module.WindowMonitor
{
    public class WindowMonitor : BaseMonitor
    {
        private readonly HardwareConfig[] _applications;
        private readonly int _maxDisplayCount;
        private WindowItem[] _windows = [];
        private readonly Dictionary<int, string> _processNameCache = [];
        private readonly Timer _cacheResetTimer;

        public WindowItem[] Windows
        {
            get => _windows;
            private set
            {
                if (_windows != value)
                {
                    if (_windows != null)
                    {
                        foreach (var w in _windows)
                        {
                            w.PropertyChanged -= WindowItem_PropertyChanged;
                        }
                    }

                    _windows = value;

                    if (_windows != null)
                    {
                        foreach (var w in _windows)
                        {
                            w.PropertyChanged += WindowItem_PropertyChanged;
                        }
                    }

                    NotifyPropertyChanged(nameof(Windows));
                }
            }
        }

        private void WindowItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
        }

        public WindowMonitor(HardwareConfig[] applications, int maxDisplayCount)
            : base("window", "Window", false)
        {
            _applications = applications;
            _maxDisplayCount = Math.Max(0, maxDisplayCount);

            _cacheResetTimer = new Timer(20 * 60 * 1000) // 20分ごとにキャッシュをリセット
            {
                AutoReset = true
            };
            _cacheResetTimer.Elapsed += (_, _) => _processNameCache.Clear();
            _cacheResetTimer.Start();

            InitializeSlots();
        }

        ~WindowMonitor()
        {
            Dispose(false);
        }

        private void InitializeSlots()
        {
            var slots = new WindowItem[_maxDisplayCount];

            for (int i = 0; i < _maxDisplayCount; i++)
            {
                slots[i] = new WindowItem() { Visibility = Visibility.Collapsed };
            }

            Windows = slots;
        }

        public static iMonitor[] GetInstances(HardwareConfig[] applications, int maxDisplayCount)
        {
            return
            [
                new WindowMonitor(applications, maxDisplayCount)
            ];
        }

        public sealed override void Update()
        {
            // 表示件数が0、または対象アプリが未設定の場合は何もしない
            if (_maxDisplayCount == 0 || _applications == null || _applications.Length == 0)
            {
                return;
            }

            // 有効な対象アプリのプロセス名を収集
            var targetNames = new HashSet<string>(
                _applications.Where(a => a.Enabled).Select(a => a.ActualName ?? a.ID ?? "")
            );

            if (targetNames.Count == 0)
            {
                return;
            }

            // 見つかったウィンドウを格納するリスト
            var found = new List<(IntPtr Hwnd, string Title, string ProcessName, bool IsMinimized)>();

            // EnumWindows はZ-order順（背面→前面）に列挙される
            NativeMethods.EnumWindows((hwnd, lParam) =>
            {
                // 非表示ウィンドウは除外
                if (!NativeMethods.IsWindowVisible(hwnd))
                {
                    return true;
                }

                // タイトルの取得（空欄は除外）
                string? title = GetWindowTitle(hwnd);
                if (string.IsNullOrEmpty(title))
                {
                    return true;
                }

                // ウィンドウのプロセスIDからプロセス名を取得
                NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);

                // キャッシュからプロセス名を検索、なければ Process.GetProcessById で取得してキャッシュに登録
                string processName;
                if (!_processNameCache.TryGetValue((int)processId, out processName!))
                {
                    try
                    {
                        var process = System.Diagnostics.Process.GetProcessById((int)processId);
                        processName = process.ProcessName;
                        _processNameCache[(int)processId] = processName;
                    }
                    catch
                    {
                        // プロセスが既に終了している等の場合は除外
                        return true;
                    }
                }

                // 対象アプリのプロセス名に含まれない場合は除外
                if (string.IsNullOrEmpty(processName) || !targetNames.Contains(processName))
                {
                    return true;
                }

                // デスクトップやタスクバーは除外 
                string className = ShowDesktop.GetWindowClass(hwnd);
                if (processName == "explorer" && className != "CabinetWClass")
                {
                    return true;
                }

                // 最小化状態を取得
                bool isMinimized = NativeMethods.IsIconic(hwnd);

                found.Add((hwnd, title, processName, isMinimized));

                // MaxDisplayCount に達したら列挙を中断
                return found.Count < _maxDisplayCount;
            }, IntPtr.Zero);

            // 背面→前面の順で列挙されたリストを反転して前面→背面にする
            //found.Reverse();

            int count = Math.Min(found.Count, _maxDisplayCount);

            // 見つかったウィンドウを固定配列のスロットに反映
            for (int i = 0; i < count; i++)
            {
                var item = _windows[i];
                item.Hwnd = found[i].Hwnd;
                item.Title = found[i].Title;
                item.ProcessName = found[i].ProcessName;
                item.IsMinimized = found[i].IsMinimized;
                item.Visibility = Visibility.Visible;
            }

            // 余ったスロットは非表示にリセット
            for (int i = count; i < _maxDisplayCount; i++)
            {
                var item = _windows[i];
                item.Visibility = Visibility.Collapsed;
                item.Hwnd = IntPtr.Zero;
                item.Title = "";
                item.ProcessName = "";
                item.IsMinimized = false;
            }
        }

        private static string? GetWindowTitle(IntPtr hwnd)
        {
            int length = NativeMethods.GetWindowTextLength(hwnd);
            if (length == 0)
            {
                return null;
            }

            Span<char> buffer = stackalloc char[length + 1];
            unsafe
            {
                fixed (char* pBuffer = buffer)
                {
                    int lengthRetuen = NativeMethods.GetWindowText(hwnd, (IntPtr)pBuffer, buffer.Length);

                    if (lengthRetuen > 0)
                    {
                        // 実際に書き込まれた長さ分だけを切り出して文字列化
                        return buffer[..lengthRetuen].ToString();
                    }
                }
            }

            return string.Empty;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _cacheResetTimer.Dispose();

                if (disposing && _windows != null)
                {
                    foreach (var w in _windows)
                    {
                        w.PropertyChanged -= WindowItem_PropertyChanged;
                    }
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
