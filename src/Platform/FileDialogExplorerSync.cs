using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace SSS.Windows
{
    public static class FileDialogExplorerSync
    {
        private const string ExplorerWindowClass = "CabinetWClass";
        private const string DialogWindowClass = "#32770";
        private const uint GW_HWNDNEXT = 2;
        private const uint GA_ROOT = 2;
        private const byte VK_RETURN = 0x0D;
        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private static readonly TimeSpan ExplorerFolderTtl = TimeSpan.FromSeconds(10);

        private static IntPtr _lastForegroundHwnd = IntPtr.Zero;
        private static string? _lastExplorerFolderPath;
        private static DateTime _lastExplorerFolderUpdatedAt = DateTime.MinValue;
        private static IntPtr _lastDialogHwnd = IntPtr.Zero;
        private static string? _lastInjectedPath;

        /// <summary>
        /// フォアグラウンド変更時に、直前の Explorer フォルダを保存してファイルダイアログへ反映する。
        /// </summary>
        public static bool OnForegroundChanged(IntPtr foregroundHwnd)
        {
            if (!Core.Settings.Instance.EnableFileDialogSync)
            {
                _lastForegroundHwnd = foregroundHwnd;
                return false;
            }

            CaptureExplorerFolder(_lastForegroundHwnd);
            _lastForegroundHwnd = foregroundHwnd;
            return TrySyncFromForeground(foregroundHwnd);
        }

        /// <summary>
        /// 指定された前面ウィンドウがファイルダイアログなら、保存済みの Explorer フォルダを反映する。
        /// </summary>
        public static bool TrySyncFromForeground(IntPtr foregroundHwnd)
        {
            IntPtr dialogHwnd = ResolveDialogHwnd(foregroundHwnd);
            if (dialogHwnd == IntPtr.Zero)
            {
                return false;
            }

            Log($"Dialog confirmed hwnd={FormatHwnd(dialogHwnd)} class={GetClassNameSafe(dialogHwnd)}");
            string? folderPath = GetRecentExplorerFolderPath();
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                Log("Recent Explorer folder is empty or expired");
                return false;
            }

            if (dialogHwnd == _lastDialogHwnd && string.Equals(folderPath, _lastInjectedPath, StringComparison.OrdinalIgnoreCase))
            {
                Log($"Skip duplicate injection dialog={FormatHwnd(dialogHwnd)} path={folderPath}");
                return true;
            }

            if (!TryInjectPath(dialogHwnd, folderPath))
            {
                Log($"Inject failed dialog={FormatHwnd(dialogHwnd)} path={folderPath}");
                return false;
            }

            _lastDialogHwnd = dialogHwnd;
            _lastInjectedPath = folderPath;
            Log($"Inject succeeded dialog={FormatHwnd(dialogHwnd)} path={folderPath}");
            return true;
        }

        /// <summary>
        /// 現在の前面ウィンドウを対象に、ファイルダイアログ同期を試みる。
        /// </summary>
        public static bool TrySyncCurrentForeground()
        {
            if (!Core.Settings.Instance.EnableFileDialogSync)
            {
                return false;
            }

            IntPtr foregroundHwnd = NativeMethods.GetForegroundWindow();
            if (foregroundHwnd == IntPtr.Zero)
            {
                return false;
            }

            if (IsSidebarWindow(foregroundHwnd))
            {
                foregroundHwnd = FindNextVisibleWindow(foregroundHwnd);
            }

            return foregroundHwnd != IntPtr.Zero && TrySyncFromForeground(foregroundHwnd);
        }

        private static IntPtr ResolveDialogHwnd(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            IntPtr rootHwnd = NativeMethods.GetAncestor(hwnd, GA_ROOT);
            if (rootHwnd != IntPtr.Zero)
            {
                hwnd = rootHwnd;
            }

            bool isDialog = IsFileDialogWindow(hwnd);
            return isDialog ? hwnd : IntPtr.Zero;
        }

        private static bool IsFileDialogWindow(IntPtr hwnd)
        {
            string className = ShowDesktop.GetWindowClass(hwnd);
            if (!string.Equals(className, DialogWindowClass, StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                AutomationElement element = AutomationElement.FromHandle(hwnd);
                AutomationElement? pathInput = FindFileDialogInputElement(element);
                return pathInput != null;
            }
            catch (ElementNotAvailableException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static void CaptureExplorerFolder(IntPtr hwnd)
        {
            if (!IsTargetExplorerWindow(hwnd))
            {
                return;
            }

            string? folderPath = GetExplorerFolderPath(hwnd);
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            _lastExplorerFolderPath = folderPath;
            _lastExplorerFolderUpdatedAt = DateTime.Now;
        }

        private static string? GetRecentExplorerFolderPath()
        {
            if (string.IsNullOrWhiteSpace(_lastExplorerFolderPath))
            {
                return null;
            }

            if (DateTime.Now - _lastExplorerFolderUpdatedAt > ExplorerFolderTtl)
            {
                Log($"Recent Explorer folder expired path={_lastExplorerFolderPath} updatedAt={_lastExplorerFolderUpdatedAt:O}");
                return null;
            }

            return _lastExplorerFolderPath;
        }

        private static IntPtr FindNextVisibleWindow(IntPtr hwnd)
        {
            IntPtr current = NativeMethods.GetWindow(hwnd, GW_HWNDNEXT);
            while (current != IntPtr.Zero)
            {
                IntPtr rootHwnd = NativeMethods.GetAncestor(current, GA_ROOT);
                if (rootHwnd != IntPtr.Zero)
                {
                    current = rootHwnd;
                }

                if (NativeMethods.IsWindowVisible(current) && !IsSidebarWindow(current))
                {
                    return current;
                }

                current = NativeMethods.GetWindow(current, GW_HWNDNEXT);
            }

            return IntPtr.Zero;
        }

        private static bool IsTargetExplorerWindow(IntPtr hwnd)
        {
            return NativeMethods.IsWindowVisible(hwnd)
                && string.Equals(ShowDesktop.GetWindowClass(hwnd), ExplorerWindowClass, StringComparison.Ordinal);
        }

        private static bool IsSidebarWindow(IntPtr hwnd)
        {
            IntPtr sidebarHwnd = App.Current?.Sidebar == null ? IntPtr.Zero : new System.Windows.Interop.WindowInteropHelper(App.Current.Sidebar).Handle;
            return sidebarHwnd != IntPtr.Zero && hwnd == sidebarHwnd;
        }

        private static string? GetExplorerFolderPath(IntPtr explorerHwnd)
        {
            string? selectedTabName = GetSelectedExplorerTabName(explorerHwnd);
            return GetExplorerFolderPathFromShellWindows(explorerHwnd, selectedTabName);
        }

        /// <summary>
        /// Explorer のタブ一覧から選択中タブの表示名を取得する。
        /// </summary>
        private static string? GetSelectedExplorerTabName(IntPtr explorerHwnd)
        {
            try
            {
                AutomationElement explorer = AutomationElement.FromHandle(explorerHwnd);
                AutomationElementCollection tabs = explorer.FindAll(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem)
                );

                for (int i = 0; i < tabs.Count; i++)
                {
                    AutomationElement tab = tabs[i];
                    if (!tab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out object pattern))
                    {
                        continue;
                    }

                    if (((SelectionItemPattern)pattern).Current.IsSelected)
                    {
                        return string.IsNullOrWhiteSpace(tab.Current.Name) ? null : tab.Current.Name.Trim();
                    }
                }
            }
            catch (ElementNotAvailableException)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }

            return null;
        }

        private static string? GetExplorerFolderPathFromShellWindows(IntPtr explorerHwnd, string? selectedTabName)
        {
            Type? shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType == null)
            {
                return null;
            }

            object? shell = null;
            object? windows = null;
            try
            {
                shell = Activator.CreateInstance(shellType);
                windows = shellType.InvokeMember("Windows", System.Reflection.BindingFlags.InvokeMethod, null, shell, null);
                if (windows == null)
                {
                    return null;
                }

                List<(string Path, string Name)> candidates = [];
                int count = (int)windows.GetType().InvokeMember("Count", System.Reflection.BindingFlags.GetProperty, null, windows, null)!;
                for (int i = 0; i < count; i++)
                {
                    object? window = windows.GetType().InvokeMember("Item", System.Reflection.BindingFlags.InvokeMethod, null, windows, [i]);
                    if (window == null)
                    {
                        continue;
                    }

                    try
                    {
                        object hwndValue = window.GetType().InvokeMember("HWND", System.Reflection.BindingFlags.GetProperty, null, window, null)!;
                        IntPtr shellHwnd = new(Convert.ToInt64(hwndValue));
                        string? locationUrl = window.GetType().InvokeMember("LocationURL", System.Reflection.BindingFlags.GetProperty, null, window, null) as string;
                        string? locationName = window.GetType().InvokeMember("LocationName", System.Reflection.BindingFlags.GetProperty, null, window, null) as string;

                        if (shellHwnd != explorerHwnd)
                        {
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(locationUrl))
                        {
                            continue;
                        }

                        string localPath = new Uri(locationUrl).LocalPath;
                        candidates.Add((localPath, locationName ?? ""));
                    }
                    finally
                    {
                        Marshal.FinalReleaseComObject(window);
                    }
                }

                if (candidates.Count == 0)
                {
                    return null;
                }

                if (!string.IsNullOrWhiteSpace(selectedTabName))
                {
                    foreach ((string path, string name) in candidates)
                    {
                        if (IsExplorerTabMatch(selectedTabName, name, path))
                        {
                            return path;
                        }
                    }
                }

                return candidates[0].Path;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                if (windows != null)
                {
                    Marshal.FinalReleaseComObject(windows);
                }

                if (shell != null)
                {
                    Marshal.FinalReleaseComObject(shell);
                }
            }

        }

        private static bool IsExplorerTabMatch(string selectedTabName, string locationName, string path)
        {
            string normalizedSelectedTabName = selectedTabName.Trim().Trim('"');
            string normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (Path.IsPathFullyQualified(normalizedSelectedTabName))
            {
                string selectedTabPath = Path.GetFullPath(normalizedSelectedTabName).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (string.Equals(selectedTabPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            if (string.Equals(selectedTabName, locationName, StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            string folderName = Path.GetFileName(normalizedPath);
            return string.Equals(selectedTabName, folderName, StringComparison.CurrentCultureIgnoreCase);
        }

        private static bool TryInjectPath(IntPtr dialogHwnd, string folderPath)
        {
            try
            {
                AutomationElement dialog = AutomationElement.FromHandle(dialogHwnd);
                dialog.SetFocus();

                //ファイル名入力フィールド
                AutomationElement? pathInput = FindFileDialogInputElement(dialog);

                if (pathInput == null)
                {
                    Log($"Inject target not found dialog={FormatHwnd(dialogHwnd)}");
                    return false;
                }

                if (!pathInput.TryGetCurrentPattern(ValuePattern.Pattern, out object pattern))
                {
                    Log($"Inject target has no ValuePattern dialog={FormatHwnd(dialogHwnd)} name={pathInput.Current.Name} automationId={pathInput.Current.AutomationId}");
                    return false;
                }

                Log($"Inject target dialog={FormatHwnd(dialogHwnd)} name={pathInput.Current.Name} automationId={pathInput.Current.AutomationId} controlType={pathInput.Current.ControlType.ProgrammaticName} path={folderPath}");
                ((ValuePattern)pattern).SetValue(folderPath);
                System.Threading.Thread.Sleep(50);
                SendEnter(pathInput, dialogHwnd);
                return true;
            }
            catch (ElementNotAvailableException)
            {
                Log($"TryInjectPath ElementNotAvailable dialog={FormatHwnd(dialogHwnd)}");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                Log($"TryInjectPath InvalidOperation dialog={FormatHwnd(dialogHwnd)} message={ex.Message}");
                return false;
            }
        }

        private static void SendEnter(AutomationElement element, IntPtr dialogHwnd)
        {
            IntPtr elementHwnd = new(element.Current.NativeWindowHandle);
            if (elementHwnd != IntPtr.Zero)
            {
                NativeMethods.PostMessage(elementHwnd, WM_KEYDOWN, VK_RETURN, IntPtr.Zero);
                NativeMethods.PostMessage(elementHwnd, WM_KEYUP, VK_RETURN, IntPtr.Zero);
                return;
            }

            NativeMethods.SetForegroundWindow(dialogHwnd);
            System.Threading.Thread.Sleep(30);
            element.SetFocus();
            System.Threading.Thread.Sleep(30);
            NativeMethods.keybd_event(VK_RETURN, 0, 0, UIntPtr.Zero);
            NativeMethods.keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        // ファイルダイアログかどうかを判定する
        // 1148 ファイル開く
        // 1001 保存
        // 1152 フォルダー開く
        // 41477 Alt+Dを送って入力フィールドになったパスナビゲータ部分 不採用
        private static AutomationElement? FindFileDialogInputElement(AutomationElement dialog)
        {

            AutomationElement? element = dialog.FindFirst(TreeScope.Descendants,
                new OrCondition(
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "1148"),
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "1001"),
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "1152")
                )
            );
            if (element == null)
            {
                return null;
            }
            bool hasValuePattern = element.TryGetCurrentPattern(ValuePattern.Pattern, out _);
            return hasValuePattern ? element : null;
        }

        private static string GetClassNameSafe(IntPtr hwnd)
        {
            return hwnd == IntPtr.Zero ? "" : ShowDesktop.GetWindowClass(hwnd);
        }

        private static string FormatHwnd(IntPtr hwnd)
        {
            return $"0x{hwnd.ToInt64():X}";
        }

        private static void Log(string message)
        {
            // Debug.WriteLine($"[FileDialogExplorerSync] {message}");
        }
    }
}
