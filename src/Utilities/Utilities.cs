using System;
using System.Drawing; // Icon, Bitmapの利用に必要
using System.Globalization;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32.TaskScheduler;
using SSS.Core;
using System.Diagnostics;
using SSS.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;


namespace SSS.Utilities
{
    public static class Image
    {
        private const uint WM_GETICON = 0x007F;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;
        private const int GCLP_HICONSM = -34; // 小アイコンのクラスインデックス
        private const int GCLP_HICON = -14;   // 大アイコンのクラスインデックス


        public static ImageSource? GetWindowImageSource(IntPtr hWnd)
        {
            Icon? winIcon = GetWindowIcon(hWnd);
            if (winIcon == null)
            {
                return null;
            }
            BitmapSource originalSource;
            IntPtr hIcon = winIcon.Handle;
            try
            {
                originalSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                    hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());


            }
            catch
            {
                return null;
            }
            finally
            {
                // 1. CreateBitmapSourceFromHIconが内部で増やしたハンドルを解放
                // (これを忘れるとGDIオブジェクトのメモリリークになります)
                NativeMethods.DestroyIcon(hIcon);
                // 2. 元のC#のIconオブジェクト自体を解放
                winIcon.Dispose();
            }
            // 縮小
            if (originalSource.PixelWidth != 16.0f)
            {
                // 2. 取得したアイコンを16x16ピクセルに強制リサイズする
                double targetWidth = 16.0;
                double targetHeight = 16.0;
                double scaleX = targetWidth / originalSource.PixelWidth;
                double scaleY = targetHeight / originalSource.PixelHeight;
                originalSource = new TransformedBitmap(originalSource, new ScaleTransform(scaleX, scaleY));
            }

            // 2. 一度 FormatConvertedBitmap で標準的な Bgra32（アルファチャンネルあり）に変換
            var bgra32Source = new FormatConvertedBitmap(originalSource, PixelFormats.Bgra32, null, 0);

            // 3. 【重要】ピクセルをスキャンして「背景色」を透明にする
            int width = bgra32Source.PixelWidth;
            int height = bgra32Source.PixelHeight;
            int stride = width * 4; // 1ピクセル4バイト (B, G, R, A)
            byte[] pixels = new byte[stride * height];
            bgra32Source.CopyPixels(pixels, stride, 0);


            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];
                byte originalAlpha = pixels[i + 3];

                // 1. もともと透明なピクセルはそのまま透明にする
                if (originalAlpha == 0) continue;

                // 2. 輝度（明るさ）を計算する (RGBからグレーの濃さを出す数式)
                // 白(255)に近づくほど高くなり、黒(0)に近づくほど低くなります
                double brightness = (0.299 * r) + (0.587 * g) + (0.114 * b);

                // 3. 背景が【黒】ベースか【白】ベースかで処理を分ける

                // パターンA：背景が「黒」のアイコンの場合（絵柄が白っぽい）
                // 明るい部分ほどクッキリ残し、黒い背景ほど透明にする
                // pixels[i + 3] = (byte)(brightness * (originalAlpha / 255.0));
                double baseAlpha = brightness * (originalAlpha / 255.0);

                // 2. ★ここで明るさをブーストする★
                // 1.2 〜 1.5 倍すると、中間の薄い部分が引き上げられてクッキリ明るくなります
                double boostFactor = 1.2;
                double finalAlpha = baseAlpha * boostFactor;

                // 3. 最大値（255）を超えないように制限をかける
                if (finalAlpha > 255)
                {
                    finalAlpha = 255;
                }
                // 新しいアルファ値を適用
                pixels[i + 3] = (byte)finalAlpha;

                /* 
                // パターンB：背景が「白」のアイコンの場合（絵柄が黒っぽい）
                // もし背景が白の場合は、上記のパターンAの行をコメントアウトして、以下の行を使ってください
                double inverted = 255.0 - brightness;
                pixels[i + 3] = (byte)(inverted * (originalAlpha / 255.0));
                */
            }

            // 4. 処理したピクセルデータから、新しい透過BitmapSourceを生成
            BitmapSource transparentSource = BitmapSource.Create(
                width, height,
                bgra32Source.DpiX, bgra32Source.DpiY,
                PixelFormats.Bgra32, null,
                pixels, stride);

            transparentSource.Freeze();
            return transparentSource;
        }

        public static Icon? GetWindowIcon(IntPtr hWnd)
        {

            IntPtr hIcon = IntPtr.Zero;

            // 1. まずウィンドウメッセージで小さいアイコンを要求
            hIcon = NativeMethods.SendMessage(hWnd, WM_GETICON, (IntPtr)ICON_SMALL, IntPtr.Zero);

            // 2. 取れなければ大きいアイコンを要求
            if (hIcon == IntPtr.Zero)
                hIcon = NativeMethods.SendMessage(hWnd, WM_GETICON, (IntPtr)ICON_BIG, IntPtr.Zero);

            // 3. それでも取れなければクラスに登録されている小さいアイコンを取得
            if (hIcon == IntPtr.Zero)
                hIcon = NativeMethods.GetClassLongPtr(hWnd, GCLP_HICONSM);

            // 4. 最後の手動手段としてクラスの大きいアイコンを取得
            if (hIcon == IntPtr.Zero)
                hIcon = NativeMethods.GetClassLongPtr(hWnd, GCLP_HICON);

            // ハンドルが取得できたらC#のIconオブジェクトに変換
            if (hIcon != IntPtr.Zero)
            {
                try
                {
                    return Icon.FromHandle(hIcon);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }
    }
    
    public static class Paths
    {
        private const string SETTINGS = "settings.json";
        public static string Install(Version version)
        {
            return Path.Combine(LocalAppDirPath, string.Format("app-{0}", version.ToString(3)));
        }

        public static string Exe(Version version)
        {
            return Path.Combine(Install(version), ExeName);
        }

        public static string CurrentDirectory
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        public static string TaskBar
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");
            }
        }

        private static string? _assemblyName { get; set; } = null;

        public static string AssemblyName
        {
            get
            {
                if (_assemblyName == null)
                {
                    _assemblyName = Assembly.GetExecutingAssembly().GetName().Name!;
                }

                return _assemblyName;
            }
        }

        private static string? _exeName { get; set; } = null;

        public static string ExeName
        {
            get
            {
                if (_exeName == null)
                {
                    _exeName = string.Format("{0}.exe", AssemblyName);
                }

                return _exeName;
            }
        }

        private static string? _settingsFile { get; set; } = null;

        public static string SettingsFile
        {
            get
            {
                if (_settingsFile == null)
                {
                    string currentDirPath = Path.Combine(Environment.CurrentDirectory, SETTINGS);
                    string localAppPath = Path.Combine(LocalAppDirPath, SETTINGS);

                    // Prefer the settings file in the current directory, if it exists
                    _settingsFile = File.Exists(currentDirPath) ? currentDirPath : localAppPath;
                }

                return _settingsFile;
            }
        }


        private static string? _localAppDirPath { get; set; } = null;
        private static string? _localIconThemesPath { get; set; } = null;

        public static string LocalAppDirPath
        {
            get
            {
                if (_localAppDirPath == null)
                {
                    _localAppDirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AssemblyName);
                }

                return _localAppDirPath;
            }
        }

        public static string LocalIconThemesPath
        {
            get
            {
                if (_localIconThemesPath == null)
                {
                    _localIconThemesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AssemblyName, "IconThemes");
                }

                return _localIconThemesPath;
            }
        }
    }

public static class Startup
{
    /// <summary>
    /// スタートアップタスクが存在し、かつ現在実行中のexeパスと一致するかを確認する。
    /// フルパスで大文字小文字を区別せず比較するため、異なるフォルダに再インストールした場合は期限切れと判定される。
    /// </summary>
    public static bool StartupTaskExists()
    {
        using (TaskService taskService = new TaskService())
        {
            Task task = taskService.FindTask(Constants.Generic.TASKNAME);

            if (task == null)
                return false;

            ExecAction? action = task.Definition.Actions.OfType<ExecAction>().FirstOrDefault();

            string currentExe = Process.GetCurrentProcess().MainModule!.FileName;

            // Check if it points to the correct exe (not a DLL or SYS)
            if (action == null || !string.Equals(action.Path, currentExe, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }
    }

    public static void EnableStartupTask(string? exePath = null)
    {
        if (StartupTaskExists())
        {
            return;
        }
        
        try
        {
            using TaskService taskService = new TaskService();
            // Remove any legacy tasks that point to wrong files
            CleanLegacyTasks(taskService);

            TaskDefinition def = taskService.NewTask();
            def.Triggers.Add(new LogonTrigger { Enabled = true });

            string targetExe = exePath ?? Process.GetCurrentProcess().MainModule!.FileName;
            def.Actions.Add(new ExecAction(targetExe));

            def.Principal.RunLevel = TaskRunLevel.Highest;
            def.Settings.DisallowStartIfOnBatteries = false;
            def.Settings.StopIfGoingOnBatteries = false;
            def.Settings.ExecutionTimeLimit = TimeSpan.Zero;

            taskService.RootFolder.RegisterTaskDefinition(Constants.Generic.TASKNAME, def);
        }
        catch (Exception e)
        {
            using (EventLog log = new EventLog("Application"))
            {
                log.Source = Strings.AppName;
                log.WriteEntry(e.ToString(), EventLogEntryType.Error, 100, 1);
            }
        }
    }

    public static void DisableStartupTask()
    {
        using (TaskService taskService = new TaskService())
        {
            taskService.RootFolder.DeleteTask(Constants.Generic.TASKNAME, false);
        }
    }

    /// <summary>
    /// Deletes legacy SolidStateSidebar tasks that point to DLL or SYS files.
    /// </summary>
    private static void CleanLegacyTasks(TaskService taskService)
    {
        string[] legacyFileNames =
        [
            "SidebarDiagnostics.exe",
            "SolidStateSidebar.exe"
        ];

        foreach (Task t in taskService.RootFolder.AllTasks)
        {
            if (t.Name.Equals(Constants.Generic.TASKNAME, StringComparison.OrdinalIgnoreCase))
            {
                var action = t.Definition.Actions.OfType<ExecAction>().FirstOrDefault();
                if (action != null && legacyFileNames.Any(lfn => string.Equals(lfn, Path.GetFileName(action.Path), StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        taskService.RootFolder.DeleteTask(t.Name, false);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to delete legacy task {t.Name}: {ex.Message}");
                    }
                }
            }
        }
    }
}

    public static class Culture
    {
        public const string DEFAULT = "Default";

        public static void SetDefault()
        {
            Default = Thread.CurrentThread.CurrentUICulture;
        }

        public static void SetCurrent(bool init)
        {
            Strings.Culture = CultureInfo;

            Thread.CurrentThread.CurrentCulture = CultureInfo;
            Thread.CurrentThread.CurrentUICulture = CultureInfo;

            if (init)
            {
                FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.Name)));
            }
        }

        public static CultureItem[] GetAll()
        {
            return new CultureItem[1] { new CultureItem() { Value = DEFAULT, Text = Strings.SettingsLanguageDefault } }.Concat(Languages.Select(lang => new CultureInfo(lang)).OrderBy(c => c.DisplayName).Select(c => new CultureItem() { Value = c.Name, Text = c.DisplayName })).ToArray();
        }

        public static string[] Languages
        {
            get
            {
                return ["en", "da", "de", "fr", "ja", "nl", "zh", "it", "ru", "fi", "es"];
            }
        }

        public static CultureInfo? Default { get; private set; }

        public static CultureInfo CultureInfo
        {
            get
            {
                string culture = Core.Settings.Instance.Culture;
                if (string.Equals(culture, DEFAULT, StringComparison.Ordinal))
                    return Default!;

                CultureInfo ci = new CultureInfo(culture);

                if (ci.IsNeutralCulture)
                    ci = CultureInfo.CreateSpecificCulture(ci.Name);

                return ci;
            }
        }
    }

    public class CultureItem
    {
        public string? Value { get; set; }

        public string? Text { get; set; }
    }
}
