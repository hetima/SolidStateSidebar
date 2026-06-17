using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using Newtonsoft.Json;
using SSS.Utilities;
using SSS.Windows;
using SSS.Styling.IconTheme;

namespace SSS.Core
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Settings : ObservableObject
    {
        private Settings() { }

        private static string[]? _iconThemeList;

        public static string[] IconThemeList
        {
            get
            {
                if (_iconThemeList != null)
                {
                    return _iconThemeList;
                }

                HashSet<string> result = [];
                string iconThemesPath = Paths.LocalIconThemesPath;

                // Ensure the directory exists
                Directory.CreateDirectory(iconThemesPath);

                // Get all local themes
                foreach (string dir in Directory.GetDirectories(iconThemesPath))
                {
                    result.Add(Path.GetFileName(dir));
                }

                // Extract embedded themes
                foreach (var theme in IconThemeData.GetAvailableThemes())
                {
                    if (theme == null || theme.Name == null || theme.Icons == null)
                    {
                        continue;
                    }
                    if (!result.Contains(theme.Name))
                    {
                        string themeDir = Path.Combine(iconThemesPath, theme.Name);
                        Directory.CreateDirectory(themeDir);

                        foreach (var icon in theme.Icons)
                        {
                            string iconPath = Path.Combine(themeDir, icon.Key + ".svg");
                            File.WriteAllText(iconPath, icon.Value);
                        }
                        result.Add(theme.Name);
                    }
                    else
                    {
                        // If the theme already exists, ensure all icons are present
                        string themeDir = Path.Combine(iconThemesPath, theme.Name);
                        foreach (var icon in theme.Icons)
                        {
                            string iconPath = Path.Combine(themeDir, icon.Key + ".svg");
                            if (!File.Exists(iconPath))
                            {
                                File.WriteAllText(iconPath, icon.Value);
                            }
                        }
                    }
                }

                // Move "Default" to the front
                if (result.Contains("Default"))
                {
                    result.Remove("Default");
                    List<string> sorted = ["Default", .. result];
                    return _iconThemeList = sorted.ToArray();
                }
                return _iconThemeList = [.. result];
            }
        }

        public void Save()
        {
            if (!Directory.Exists(Paths.LocalAppDirPath))
            {
                Directory.CreateDirectory(Paths.LocalAppDirPath);
            }

            using (StreamWriter _writer = File.CreateText(Paths.SettingsFile))
            {
                new JsonSerializer() { Formatting = Formatting.Indented }.Serialize(_writer, this);
            }
        }

        public void Reload()
        {
            _instance = Load();
        }

        private static Settings Load()
        {
            Settings? _return = null;

            if (File.Exists(Paths.SettingsFile))
            {
                using StreamReader _reader = File.OpenText(Paths.SettingsFile);
                _return = (Settings)new JsonSerializer().Deserialize(_reader, typeof(Settings))!;
            }

            // Extract embedded icon themes
            _ = Core.Settings.IconThemeList;

            return _return ?? new Settings();
        }


        private bool _initialSetup = true;

        [JsonProperty]
        public bool InitialSetup
        {
            get => _initialSetup;
            set => SetProperty(ref _initialSetup, value);
        }

        private DockEdge _dockEdge = DockEdge.Right;

        [JsonProperty]
        public DockEdge DockEdge
        {
            get => _dockEdge;
            set => SetProperty(ref _dockEdge, value);
        }

        private int _screenIndex = 0;

        [JsonProperty]
        public int ScreenIndex
        {
            get => _screenIndex;
            set => SetProperty(ref _screenIndex, value);
        }

        private string _culture = Utilities.Culture.DEFAULT;

        [JsonProperty]
        public string Culture
        {
            get => _culture;
            set => SetProperty(ref _culture, value);
        }

        private bool _useAppBar = true;
        
        [JsonProperty]
        public bool UseAppBar
        {
            get => _useAppBar;
            set => SetProperty(ref _useAppBar, value);
        }

        private bool _alwaysTop = true;

        [JsonProperty]
        public bool AlwaysTop
        {
            get => _alwaysTop;
            set => SetProperty(ref _alwaysTop, value);
        }


        private bool _runAtStartup = true;

        [JsonProperty]
        public bool RunAtStartup
        {
            get => _runAtStartup;
            set => SetProperty(ref _runAtStartup, value);
        }

        private double _uiScale = 1d;

        [JsonProperty]
        public double UIScale
        {
            get => _uiScale;
            set => SetProperty(ref _uiScale, value);
        }

        private int _xOffset = 0;

        [JsonProperty]
        public int XOffset
        {
            get => _xOffset;
            set => SetProperty(ref _xOffset, value);
        }

        private int _yOffset = 0;

        [JsonProperty]
        public int YOffset
        {
            get => _yOffset;
            set => SetProperty(ref _yOffset, value);
        }

        private int _pollingInterval = 1000;

        [JsonProperty]
        public int PollingInterval
        {
            get => _pollingInterval;
            set => SetProperty(ref _pollingInterval, value);
        }

        private bool _toolbarMode = true;

        [JsonProperty]
        public bool ToolbarMode
        {
            get => _toolbarMode;
            set => SetProperty(ref _toolbarMode, value);
        }

        private bool _clickThrough = false;

        [JsonProperty]
        public bool ClickThrough
        {
            get => _clickThrough;
            set => SetProperty(ref _clickThrough, value);
        }

        private bool _showTrayIcon = true;

        [JsonProperty]
        public bool ShowTrayIcon
        {
            get => _showTrayIcon;
            set => SetProperty(ref _showTrayIcon, value);
        }

        private bool _enableFileDialogSync = false;

        [JsonProperty]
        public bool EnableFileDialogSync
        {
            get => _enableFileDialogSync;
            set => SetProperty(ref _enableFileDialogSync, value);
        }

        private bool _collapseMenuBar = false;

        [JsonProperty]
        public bool CollapseMenuBar
        {
            get => _collapseMenuBar;
            set => SetProperty(ref _collapseMenuBar, value);
        }

        private bool _initiallyHidden = false;

        [JsonProperty]
        public bool InitiallyHidden
        {
            get => _initiallyHidden;
            set => SetProperty(ref _initiallyHidden, value);
        }

        private int _sidebarMargin = 15;

        [JsonProperty]
        public int SidebarMargin
        {
            get => _sidebarMargin;
            set => SetProperty(ref _sidebarMargin, value);
        }

        private int _sidebarWidth = 180;

        [JsonProperty]
        public int SidebarWidth
        {
            get => _sidebarWidth;
            set => SetProperty(ref _sidebarWidth, value);
        }

        private string _bgColor = "#1F1F1F";

        [JsonProperty]
        public string BGColor
        {
            get => _bgColor;
            set => SetProperty(ref _bgColor, value);
        }

        private double _bgOpacity = 0.85d;

        [JsonProperty]
        public double BGOpacity
        {
            get => _bgOpacity;
            set => SetProperty(ref _bgOpacity, value);
        }

        private TextAlign _textAlign = TextAlign.Left;

        [JsonProperty]
        public TextAlign TextAlign
        {
            get => _textAlign;
            set => SetProperty(ref _textAlign, value);
        }

        private int _fontSize = 14;

        [JsonProperty]
        public int FontSize
        {
            get
            {
                return _fontSize;
            }
            set
            {
                _fontSize = value;

                NotifyPropertyChanged(nameof(FontSize));
                NotifyPropertyChanged(nameof(TitleFontSize));
                NotifyPropertyChanged(nameof(SmallFontSize));
                NotifyPropertyChanged(nameof(IconSize));
                NotifyPropertyChanged(nameof(BarHeight));
                NotifyPropertyChanged(nameof(BarWidth));
                NotifyPropertyChanged(nameof(BarWidthWide));
            }
        }

        [JsonIgnore]
        public int TitleFontSize => FontSize + 2;

        [JsonIgnore]
        public int SmallFontSize => Math.Max(FontSize - 2, 8);

        [JsonIgnore]
        public int IconSize => FontSize + 10;

        [JsonIgnore]
        public int SmallIconSize => FontSize + 2;
        [JsonIgnore]
        public int BarHeight => Math.Max(FontSize - 3, 4);

        [JsonIgnore]
        public int BarWidth => BarHeight * 6;

        [JsonIgnore]
        public int BarWidthWide => BarHeight * 8;

        private string _fontName = "Arial";

        [JsonProperty]
        public string FontName
        {
            get => _fontName;
            set => SetProperty(ref _fontName, value);
        }


        private string _fontColor = "#FFFFFF";

        private Color _fontColorColor;

        [JsonIgnore]
        public Color FontColorColor
        {
            get
            {
                return _fontColorColor;
            }
        }

        [JsonProperty]
        public string FontColor
        {
            get
            {
                return _fontColor;
            }
            set
            {
                _fontColor = value;
                _fontColorColor = (Color)ColorConverter.ConvertFromString(_fontColor);

                NotifyPropertyChanged(nameof(FontColor));
                NotifyPropertyChanged(nameof(FontColorColor));
            }
        }

        private string _alertFontColor = "#FF4136";

        [JsonProperty]
        public string AlertFontColor
        {
            get => _alertFontColor;
            set => SetProperty(ref _alertFontColor, value);
        }

        private bool _alertBlink = true;

        [JsonProperty]
        public bool AlertBlink
        {
            get => _alertBlink;
            set => SetProperty(ref _alertBlink, value);
        }

        private bool _metricsNoWrap = false;

        [JsonProperty]
        public bool MetricsNoWrap
        {
            get => _metricsNoWrap;
            set => SetProperty(ref _metricsNoWrap, value);
        }

        private string _iconTheme = "Default";

        [JsonProperty]
        public string IconTheme
        {
            get => _iconTheme;
            set => SetProperty(ref _iconTheme, value);
        }

        public string? GetIconSvgPath(string iconName)
        {
            string path = Path.Combine(Paths.LocalIconThemesPath, IconTheme, iconName + ".svg");
            return File.Exists(path) ? path : null;
        }

        private bool _showMachineName = false;

        [JsonProperty]
        public bool ShowMachineName
        {
            get => _showMachineName;
            set => SetProperty(ref _showMachineName, value);
        }

        private Dictionary<string, IModuleData>? _modules = null;

        [JsonProperty("Modules")]
        [JsonConverter(typeof(ModuleDataConverter))]
        public Dictionary<string, IModuleData>? Modules
        {
            get => _modules;
            set => SetProperty(ref _modules, value);
        }

        public static Dictionary<string, IModuleData> CheckModules(Dictionary<string, IModuleData>? modules)
        {
            Dictionary<string, IModuleData> defaults = ModuleDataConverter.GetDefaults();

            if (modules == null)
            {
                return defaults;
            }

            foreach (var kvp in defaults)
            {
                if (!modules.TryGetValue(kvp.Key, out IModuleData? existing))
                {
                    modules[kvp.Key] = kvp.Value;
                }
                else
                {
                    IModuleData def = kvp.Value;

                    if (existing.Hardware == null)
                    {
                        existing.Hardware = def.Hardware;
                    }

                    if (existing.Metrics == null)
                    {
                        existing.Metrics = def.Metrics;
                    }
                    else
                    {
                        existing.Metrics = new ObservableCollection<MetricConfig>(
                            from d in def.Metrics
                            join m in existing.Metrics on d.Key equals m.Key into merged
                            from newm in merged.DefaultIfEmpty(d)
                            select newm
                            );
                    }
                }
            }

            return modules;
        }

        private Hotkey[] _hotkeys = [];

        [JsonProperty]
        public Hotkey[] Hotkeys
        {
            get => _hotkeys;
            set => SetProperty(ref _hotkeys, value);
        }

        private static Settings? _instance = null;

        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }

                return _instance;
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class DateSetting
    {
        internal DateSetting() { }

        private DateSetting(string format)
        {
            Format = format;
        }

        [JsonProperty]
        public string? Format { get; set; }

        public string Display
        {
            get
            {
                if (string.Equals(Format, "Disabled", StringComparison.Ordinal))
                {
                    return Strings.SettingsDateFormatDisabled;
                }

                return DateTime.Today.ToString(Format, Culture.Default);
            }
        }

        public override bool Equals(object? obj)
        {
            DateSetting? _that = obj as DateSetting;

            if (_that == null)
            {
                return false;
            }

            return string.Equals(this.Format, _that.Format, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static readonly DateSetting Disabled = new DateSetting("");
        public static readonly DateSetting Short = new DateSetting("M");
        public static readonly DateSetting Normal = new DateSetting("d");
        public static readonly DateSetting Long = new DateSetting("D");
    }
}
