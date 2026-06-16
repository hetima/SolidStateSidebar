using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using SSS.Core;
using SSS.Windows;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SSS.Module.WindowMonitor
{
    public class WindowMonitor : BaseMonitor
    {
        private readonly HardwareConfig[] _applications; //いらない
        private readonly HashSet<string> _applicationNames;
        private readonly int _maxDisplayCount;
        private readonly bool _scrollToSwitch;
        private WindowItem[] _windows = [];
        private Dictionary<uint, string> _processNameCache = [];
        private Dictionary<uint, ImageSource> _processIconCache = [];
        private DateTime _lastCacheClearTime = DateTime.MinValue;
        private DateTime _lastFullRefreshTime = DateTime.MinValue;
        private CancellationTokenSource? _switchHighlightCancellation;
        private IntPtr _lastSwitchedHwnd = IntPtr.Zero;

        private static readonly TimeSpan EmptyRefreshInterval = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan FullRefreshInterval = TimeSpan.FromSeconds(30);

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

        public WindowMonitor(HardwareConfig[] applications, int maxDisplayCount, bool scrollToSwitch)
            : base("window", "Window", false)
        {
            _applications = applications;
            // 有効な対象アプリのプロセス名を収集
            _applicationNames = new HashSet<string>(
                _applications.Where(a => a.Enabled).Select(a => a.ActualName ?? a.ID ?? "")
            );
            _maxDisplayCount = Math.Max(0, maxDisplayCount);
            _scrollToSwitch = scrollToSwitch;

            InitializeSlots();
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

        public static iMonitor[] GetInstances(HardwareConfig[] applications, int maxDisplayCount, bool scrollToSwitch)
        {
            return
            [
                new WindowMonitor(applications, maxDisplayCount, scrollToSwitch)
            ];
        }

        /// <summary>
        /// マウスホイール操作で対象ウィンドウを順送りする。
        /// </summary>
        public bool TryScrollSwitch(int delta)
        {
            if (!_scrollToSwitch || !IsPointerOnSidebar())
            {
                return false;
            }

            WindowItem[] visibleWindows = _windows
                .Where(w => w.Visibility == Visibility.Visible && w.Hwnd != IntPtr.Zero)
                .ToArray();
            if (visibleWindows.Length <= 1)
            {
                return false;
            }

            int direction = delta < 0 ? 1 : -1;
            int currentIndex = Array.FindIndex(visibleWindows, w => w.Hwnd == _lastSwitchedHwnd);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int targetIndex = (currentIndex + direction + visibleWindows.Length) % visibleWindows.Length;
            if (targetIndex == currentIndex)
            {
                return false;
            }

            WindowItem target = visibleWindows[targetIndex];

            _lastSwitchedHwnd = target.Hwnd;
            SetSwitchHighlight(target);
            WindowHelper.ActivateWindow(target.Hwnd);
            return true;
        }

        public sealed override void Update()
        {
            if (_maxDisplayCount == 0 || _applications == null || _applications.Length == 0)
            {
                return;
            }

            if(_lastSwitchedHwnd != IntPtr.Zero)
            {
                RefreshWindows(null, ignoreFrontWindowCheck: true);
                return;
            }

            var now = DateTime.Now;
            bool hasVisibleWindows = HasVisibleWindows();
            TimeSpan refreshInterval = hasVisibleWindows ? FullRefreshInterval : EmptyRefreshInterval;
            if (now - _lastFullRefreshTime < refreshInterval)
            {
                return;
            }

            RefreshWindows(null, ignoreFrontWindowCheck: true);
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

            RefreshWindows(frontHwnd, ignoreFrontWindowCheck: false);
        }

        /// <summary>
        /// 必要最小限の条件で対象ウィンドウ一覧を更新する。
        /// </summary>
        private void RefreshWindows(IntPtr? frontHwnd, bool ignoreFrontWindowCheck)
        {

            if (IsPointerOnSidebar())
            {
                return;
            }

            
            var targetNames = _applicationNames;
            if (targetNames.Count == 0)
            {
                return;
            }

            bool hasVisibleWindows = HasVisibleWindows();

            // キャッシュにfrontHwndが含まれていたら、監視対象アプリでなければearly return
            if (_lastSwitchedHwnd == IntPtr.Zero && !ignoreFrontWindowCheck && hasVisibleWindows && _processNameCache.Count > 0 && frontHwnd != null)
            {
                NativeMethods.GetWindowThreadProcessId((IntPtr)frontHwnd, out uint processId);
                _processNameCache.TryGetValue(processId, out string? frontAppName);
                if (!string.IsNullOrEmpty(frontAppName) && !targetNames.Contains(frontAppName))
                {
                    return;
                }
            }

            // 30分経過時にキャッシュをリフレッシュ（使用されたエントリのみ新しいキャッシュに移し替える）
            Dictionary<uint, string>? processNameNewCache = null;
            Dictionary<uint, ImageSource>? processIconNewCache = null;
            if (DateTime.Now - _lastCacheClearTime > TimeSpan.FromMinutes(30))
            {
                processNameNewCache = [];
                processIconNewCache = [];
                _lastCacheClearTime = DateTime.Now;
            }
            
            // _lastSwitchedHwnd の破棄は IsPointerOnSidebar() と targetNames.Count の後です。この順序で今回の目的には合っていますが、もし今後「対象アプリ設定を空にした直後」みたいな分岐でも必ず破棄したいなら、破棄位置は再検討の余地があります。
            _lastSwitchedHwnd = IntPtr.Zero;

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
                        processNameNewCache?[processId] = processName;
                        ImageSource? icon = Utilities.Image.GetWindowImageSource(hwnd);
                        if (icon != null)
                        {
                            _processIconCache[processId] = icon;
                            processIconNewCache?[processId] = icon;
                        }
                    }
                    catch
                    {
                        // プロセスが既に終了している等の場合は除外
                        return true;
                    }
                }
                else if (processNameNewCache != null)
                {
                    // キャッシュヒット時は新しいキャッシュにコピー（使うものを拾い出す）
                    processNameNewCache[processId] = processName;
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
                if (processIconNewCache != null && pRocessicon != null)
                {
                    processIconNewCache[processId] = pRocessicon;
                }
                found.Add((hwnd, title, processName, isMinimized, pRocessicon));

                // MaxDisplayCount に達したら列挙を中断
                return found.Count < _maxDisplayCount;
            }, IntPtr.Zero);

            // キャッシュリフレッシュ: 新しいキャッシュと入れ替え（古いキャッシュはGC回収）
            if (processNameNewCache != null)
            {
                _processNameCache = processNameNewCache;
                _processIconCache = processIconNewCache!;
            }

            _lastFullRefreshTime = DateTime.Now;

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

        /// <summary>
        /// 現在表示中のスロットがあるかを返す。
        /// </summary>
        private bool HasVisibleWindows()
        {
            return _windows.Any(w => w.Visibility == Visibility.Visible && w.Hwnd != IntPtr.Zero);
        }

        /// <summary>
        /// ホイール操作で切り替えた項目を短時間強調表示する。
        /// </summary>
        private async void SetSwitchHighlight(WindowItem target)
        {
            _switchHighlightCancellation?.Cancel();
            _switchHighlightCancellation?.Dispose();
            _switchHighlightCancellation = new CancellationTokenSource();
            CancellationToken cancellationToken = _switchHighlightCancellation.Token;

            foreach (WindowItem window in _windows)
            {
                window.IsSwitchHighlighted = false;
            }

            target.IsSwitchHighlighted = true;

            try
            {
                await Task.Delay(800, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                target.IsSwitchHighlighted = false;
            }
        }

        /// <summary>
        /// マウスポインタが Sidebar 上にある間は更新を止める。
        /// </summary>
        private static bool IsPointerOnSidebar()
        {
            if (Application.Current?.Dispatcher == null)
            {
                return false;
            }

            if (Application.Current.Dispatcher.CheckAccess())
            {
                return App.Current.Sidebar?.IsMouseOver == true;
            }

            return Application.Current.Dispatcher.Invoke(() => App.Current.Sidebar?.IsMouseOver == true);
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
                if (disposing && _windows != null)
                {
                    foreach (var w in _windows)
                    {
                        w.PropertyChanged -= WindowItem_PropertyChanged;
                    }
                }

                if (disposing)
                {
                    _switchHighlightCancellation?.Cancel();
                    _switchHighlightCancellation?.Dispose();
                    _switchHighlightCancellation = null;
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
