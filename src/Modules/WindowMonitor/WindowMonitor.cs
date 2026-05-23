using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using System.Windows;
using SSS.Core;
using SSS.Windows;
using System.Diagnostics;

namespace SSS.Module.WindowMonitor
{
    public class WindowMonitor : BaseMonitor
    {
        private readonly HardwareConfig[] _applications; //いらない
        private readonly HashSet<string> _applicationNames;
        private readonly int _maxDisplayCount;
        private WindowItem[] _windows = [];
        private readonly Dictionary<uint, string> _processNameCache = [];
        private readonly Dictionary<uint, ImageSource> _processIconCache = [];
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
            // 有効な対象アプリのプロセス名を収集
            _applicationNames = new HashSet<string>(
                _applications.Where(a => a.Enabled).Select(a => a.ActualName ?? a.ID ?? "")
            );
            _maxDisplayCount = Math.Max(0, maxDisplayCount);

            _cacheResetTimer = new Timer(20 * 60 * 1000) // 20分ごとにキャッシュをリセット
            {
                AutoReset = true
            };
            // System.Timers.Timer の Elapsed は ThreadPool スレッドで発火するが、
            // Dictionary はスレッドセーフではないため、UI スレッドでクリアする
            _cacheResetTimer.Elapsed += (_, _) =>
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    _processNameCache.Clear();
                    _processIconCache.Clear();
                });
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
            
        }

        public void UpdateFromHook(IntPtr? frontHwnd)
        {
            // 表示件数が0、または対象アプリが未設定の場合は何もしない
            if (_maxDisplayCount == 0 || _applications == null || _applications.Length == 0)
            {
                return;
            }

            // 有効な対象アプリのプロセス名を収集
            var targetNames = _applicationNames;

            if (targetNames.Count == 0)
            {
                return;
            }

            // 最初かキャッシュクリア後のスキャンならフルスキャン
            // キャッシュにfrontHwndが含まれてなかったらフルスキャン
            // キャッシュにfrontHwndが含まれていたら、監視対象アプリでなければearly return
            if (_processNameCache.Count() > 0 && frontHwnd != null)
            {
                NativeMethods.GetWindowThreadProcessId((IntPtr)frontHwnd, out uint processId);
                _processNameCache.TryGetValue(processId, out string? frontAppName);
                if (!string.IsNullOrEmpty(frontAppName) && !targetNames.Contains(frontAppName))
                {
                    return;
                }
            }


            // 見つかったウィンドウを格納するリスト
            var found = new List<(IntPtr Hwnd, string Title, string ProcessName, bool IsMinimized, ImageSource? ProcessIcon)>();

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
                if (!_processNameCache.TryGetValue(processId, out processName!))
                {
                    try
                    {
                        var process = System.Diagnostics.Process.GetProcessById((int)processId);
                        processName = process.ProcessName;
                        _processNameCache[processId] = processName;
                        ImageSource? icon = Utilities.Image.GetWindowImageSource(hwnd);
                        if (icon != null)
                        {
                            _processIconCache[processId] = icon;
                        }
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
                // タイトルを整形
                title = SanitizeWindowTitle(title, processName);

                _processIconCache.TryGetValue(processId, out ImageSource? pRocessicon);
                found.Add((hwnd, title, processName, isMinimized, pRocessicon));

                // MaxDisplayCount に達したら列挙を中断
                return found.Count < _maxDisplayCount;
            }, IntPtr.Zero);

            // 背面→前面の順で列挙されたリストを反転して前面→背面にする
            //found.Reverse();

            int count = Math.Min(found.Count, _maxDisplayCount);

            // 見つかったウィンドウを固定配列のスロットに反映
            for (int i = 0; i < count; i++)
            {
                WindowItem item = _windows[i];
                item.Hwnd = found[i].Hwnd;
                item.Title = found[i].Title;
                item.ProcessName = found[i].ProcessName;
                item.IsMinimized = found[i].IsMinimized;
                item.Visibility = Visibility.Visible;
                item.ProcessIcon = found[i].ProcessIcon;
            }

            // 余ったスロットは非表示にリセット
            for (int i = count; i < _maxDisplayCount; i++)
            {
                WindowItem item = _windows[i];
                item.Visibility = Visibility.Collapsed;
                item.Hwnd = IntPtr.Zero;
                item.Title = "";
                item.ProcessName = "";
                item.IsMinimized = false;
                item.ProcessIcon = null;
            }
        }

        private static List<string> _CodeLikeApps = ["Code", "Code - Insiders", "VSCodium"];

        // タイトルに" - "が含まれている場合、末尾の " - 任意" を削除して短縮する
        // processNameが"code"などの場合末尾を残す
        private static string SanitizeWindowTitle(string title, string processName)
        {
            // タイトルに" - "が含まれている場合、末尾の " - 任意" を削除して短縮する
            // processNameが "Code" などの場合末尾を残す
            int index = title.LastIndexOf(" - ");
            if (index > 0)
            {
                if (_CodeLikeApps.Contains(processName, StringComparer.OrdinalIgnoreCase))
                {
                    title = title[(index + 3)..];
                }
                else
                {
                    title = title[..index];
                }
            }
            return title;
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
