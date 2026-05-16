using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using SidebarDiagnostics.Utilities;
using SidebarDiagnostics.Core;
using SidebarDiagnostics.Windows;
using System.Windows.Media;
using System.Drawing.Text;

namespace SidebarDiagnostics.Models
{
    public class SettingsModel : INotifyPropertyChanged
    {
        public SettingsModel(Sidebar sidebar)
        {
            DockEdgeItems = new DockItem[2]
            {
                new DockItem() { Text = Strings.SettingsDockLeft, Value = DockEdge.Left },
                new DockItem() { Text = Strings.SettingsDockRight, Value = DockEdge.Right }
            };

            DockEdge = Core.Settings.Instance.DockEdge;

            Monitor[] _monitors = Monitor.GetMonitors();

            ScreenItems = _monitors.Select((s, i) => new ScreenItem() { Index = i, Text = string.Format("#{0}", i + 1) }).ToArray();

            if (Core.Settings.Instance.ScreenIndex < _monitors.Length)
            {
                ScreenIndex = Core.Settings.Instance.ScreenIndex;
            }
            else
            {
                ScreenIndex = _monitors.Where(s => s.IsPrimary).Select((s, i) => i).Single();
            }

            CultureItems = Utilities.Culture.GetAll();
            Culture = Core.Settings.Instance.Culture;

            UIScale = Core.Settings.Instance.UIScale;
            XOffset = Core.Settings.Instance.XOffset;
            YOffset = Core.Settings.Instance.YOffset;
            PollingInterval = Core.Settings.Instance.PollingInterval;
            UseAppBar = Core.Settings.Instance.UseAppBar;
            AlwaysTop = Core.Settings.Instance.AlwaysTop;
            ToolbarMode = Core.Settings.Instance.ToolbarMode;
            ClickThrough = Core.Settings.Instance.ClickThrough;
            ShowTrayIcon = Core.Settings.Instance.ShowTrayIcon;
            RunAtStartup = Core.Settings.Instance.RunAtStartup;
            SidebarWidth = Core.Settings.Instance.SidebarWidth;
            AutoBGColor = Core.Settings.Instance.AutoBGColor;
            BGColor = Core.Settings.Instance.BGColor;
            BGOpacity = Core.Settings.Instance.BGOpacity;

            TextAlignItems = new TextAlignItem[2]
            {
                new TextAlignItem() { Text = Strings.SettingsTextAlignLeft, Value = TextAlign.Left },
                new TextAlignItem() { Text = Strings.SettingsTextAlignRight, Value = TextAlign.Right }
            };

            TextAlign = Core.Settings.Instance.TextAlign;

            FontSettingItems =
            [
                FontSetting.x10,
                FontSetting.x12,
                FontSetting.x14,
                FontSetting.x16,
                FontSetting.x18,
                FontSetting.x20,
                FontSetting.x22,
                FontSetting.x24,
            ];

            FontNameItems = Fonts.SystemFontFamilies.Select(i => i.Source).ToArray();

            FontSetting = Core.Settings.Instance.FontSetting;
            FontName = Core.Settings.Instance.FontName;
            FontColor = Core.Settings.Instance.FontColor;
            AlertFontColor = Core.Settings.Instance.AlertFontColor;
            AlertBlink = Core.Settings.Instance.AlertBlink;
            IconThemeItems = Core.Settings.IconThemeList;
            IconTheme = Core.Settings.Instance.IconTheme;

            DateSettingItems = new DateSetting[4]
            {
                DateSetting.Disabled,
                DateSetting.Short,
                DateSetting.Normal,
                DateSetting.Long
            };

            DateSetting = Core.Settings.Instance.DateSetting;
            CollapseMenuBar = Core.Settings.Instance.CollapseMenuBar;
            InitiallyHidden = Core.Settings.Instance.InitiallyHidden;
            ShowMachineName = Core.Settings.Instance.ShowMachineName;
            ShowClock = Core.Settings.Instance.ShowClock;
            Clock24HR = Core.Settings.Instance.Clock24HR;

            ObservableCollection<MonitorConfig> _config = new ObservableCollection<MonitorConfig>(Core.Settings.Instance.MonitorConfig.Select(c => c.Clone()).OrderByDescending(c => c.Order));

            if (sidebar.Ready)
            {
                foreach (MonitorConfig _record in _config)
                {
                    _record.HardwareOC = new ObservableCollection<HardwareConfig>(
                        from hw in sidebar.Model.MonitorManager.GetHardware(_record.Type)
                        join config in _record.Hardware on hw.ID equals config.ID into merged
                        from newhw in merged.DefaultIfEmpty(hw).Select(newhw => { newhw.ActualName = hw.ActualName; if (string.IsNullOrEmpty(newhw.Name)) { newhw.Name = hw.ActualName; } return newhw; })
                        orderby newhw.Order descending, newhw.Name ascending
                        select newhw
                        );
                }
            }

            MonitorConfig = _config;

            if (Core.Settings.Instance.Hotkeys != null)
            {
                ToggleKey = Core.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Toggle);
                ShowKey = Core.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Show);
                HideKey = Core.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Hide);
                ReloadKey = Core.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Reload);
                CloseKey = Core.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Close);
                CycleEdgeKey = Core.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.CycleEdge);
                CycleScreenKey = Core.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.CycleScreen);
                ReserveSpaceKey = Core.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.ReserveSpace);
            }

            IsChanged = false;
        }

        public void Save()
        {
            if (!string.Equals(Culture, Core.Settings.Instance.Culture, StringComparison.Ordinal))
            {
                MessageBox.Show(Strings.LanguageChangedText, Strings.LanguageChangedTitle, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
            }

            Core.Settings.Instance.DockEdge = DockEdge;
            Core.Settings.Instance.ScreenIndex = ScreenIndex;
            Core.Settings.Instance.Culture = Culture;
            Core.Settings.Instance.UIScale = UIScale;
            Core.Settings.Instance.XOffset = XOffset;
            Core.Settings.Instance.YOffset = YOffset;
            Core.Settings.Instance.PollingInterval = PollingInterval;
            Core.Settings.Instance.UseAppBar = UseAppBar;
            Core.Settings.Instance.AlwaysTop = AlwaysTop;
            Core.Settings.Instance.ToolbarMode = ToolbarMode;
            Core.Settings.Instance.ClickThrough = ClickThrough;
            Core.Settings.Instance.ShowTrayIcon = ShowTrayIcon;
            Core.Settings.Instance.RunAtStartup = RunAtStartup;
            Core.Settings.Instance.SidebarWidth = SidebarWidth;
            Core.Settings.Instance.AutoBGColor = AutoBGColor;
            Core.Settings.Instance.BGColor = BGColor;
            Core.Settings.Instance.BGOpacity = BGOpacity;
            Core.Settings.Instance.TextAlign = TextAlign;
            Core.Settings.Instance.FontSetting = FontSetting;
            Core.Settings.Instance.FontName = FontName;
            Core.Settings.Instance.FontColor = FontColor;
            Core.Settings.Instance.AlertFontColor = AlertFontColor;
            Core.Settings.Instance.AlertBlink = AlertBlink;
            Core.Settings.Instance.IconTheme = IconTheme;
            Core.Settings.Instance.DateSetting = DateSetting;
            Core.Settings.Instance.CollapseMenuBar = CollapseMenuBar;
            Core.Settings.Instance.InitiallyHidden = InitiallyHidden;
            Core.Settings.Instance.ShowMachineName = ShowMachineName;
            Core.Settings.Instance.ShowClock = ShowClock;
            Core.Settings.Instance.Clock24HR = Clock24HR;

            MonitorConfig[] _config = MonitorConfig.Select(c => c.Clone()).ToArray();

            for (int i = 0; i < _config.Length; i++)
            {
                HardwareConfig[] _hardware = _config[i].HardwareOC.ToArray();

                for (int v = 0; v < _hardware.Length; v++)
                {
                    _hardware[v].Order = Convert.ToByte(_hardware.Length - v);

                    if (string.IsNullOrEmpty(_hardware[v].Name) || string.Equals(_hardware[v].Name, _hardware[v].ActualName, StringComparison.Ordinal))
                    {
                        _hardware[v].Name = null;
                    }
                }

                _config[i].Hardware = _hardware;
                _config[i].HardwareOC = null;

                _config[i].Order = Convert.ToByte(_config.Length - i);
            }

            Core.Settings.Instance.MonitorConfig = _config;

            List<Hotkey> _hotkeys = new List<Hotkey>();

            if (ToggleKey != null)
            {
                _hotkeys.Add(ToggleKey);
            }
            
            if (ShowKey != null)
            {
                _hotkeys.Add(ShowKey);
            }

            if (HideKey != null)
            {
                _hotkeys.Add(HideKey);
            }

            if (ReloadKey != null)
            {
                _hotkeys.Add(ReloadKey);
            }

            if (CloseKey != null)
            {
                _hotkeys.Add(CloseKey);
            }

            if (CycleEdgeKey != null)
            {
                _hotkeys.Add(CycleEdgeKey);
            }

            if (CycleScreenKey != null)
            {
                _hotkeys.Add(CycleScreenKey);
            }

            if (ReserveSpaceKey != null)
            {
                _hotkeys.Add(ReserveSpaceKey);
            }

            Core.Settings.Instance.Hotkeys = _hotkeys.ToArray();

            Core.Settings.Instance.Save();

            App.RefreshIcon();

            if (RunAtStartup)
            {
                Startup.EnableStartupTask();
            }
            else
            {
                Startup.DisableStartupTask();
            }

            IsChanged = false;
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }

            if (propertyName != "IsChanged")
            {
                IsChanged = true;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Child_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IsChanged = true;
        }

        private void Child_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IsChanged = true;
        }

        private bool _isChanged { get; set; } = false;

        public bool IsChanged
        {
            get
            {
                return _isChanged;
            }
            set
            {
                _isChanged = value;

                NotifyPropertyChanged("IsChanged");
            }
        }

        private DockEdge _dockEdge { get; set; }

        public DockEdge DockEdge
        {
            get
            {
                return _dockEdge;
            }
            set
            {
                _dockEdge = value;

                NotifyPropertyChanged("DockEdge");
            }
        }

        private DockItem[] _dockEdgeItems { get; set; }

        public DockItem[] DockEdgeItems
        {
            get
            {
                return _dockEdgeItems;
            }
            set
            {
                _dockEdgeItems = value;

                NotifyPropertyChanged("DockEdgeItems");
            }
        }

        private int _screenIndex { get; set; }

        public int ScreenIndex
        {
            get
            {
                return _screenIndex;
            }
            set
            {
                _screenIndex = value;

                NotifyPropertyChanged("ScreenIndex");
            }
        }

        private ScreenItem[] _screenItems { get; set; }

        public ScreenItem[] ScreenItems
        {
            get
            {
                return _screenItems;
            }
            set
            {
                _screenItems = value;

                NotifyPropertyChanged("ScreenItems");
            }
        }

        private string _culture { get; set; }

        public string Culture
        {
            get
            {
                return _culture;
            }
            set
            {
                _culture = value;

                NotifyPropertyChanged("Culture");
            }
        }

        private CultureItem[] _cultureItems { get; set; }

        public CultureItem[] CultureItems
        {
            get
            {
                return _cultureItems;
            }
            set
            {
                _cultureItems = value;

                NotifyPropertyChanged("CultureItems");
            }
        }

        private double _uiScale { get; set; }

        public double UIScale
        {
            get
            {
                return _uiScale;
            }
            set
            {
                _uiScale = value;

                NotifyPropertyChanged("UIScale");
            }
        }

        private int _xOffset { get; set; }

        public int XOffset
        {
            get
            {
                return _xOffset;
            }
            set
            {
                _xOffset = value;

                NotifyPropertyChanged("XOffset");
            }
        }

        private int _yOffset { get; set; }

        public int YOffset
        {
            get
            {
                return _yOffset;
            }
            set
            {
                _yOffset = value;

                NotifyPropertyChanged("YOffset");
            }
        }

        private int _pollingInterval { get; set; }

        public int PollingInterval
        {
            get
            {
                return _pollingInterval;
            }
            set
            {
                _pollingInterval = value;

                NotifyPropertyChanged("PollingInterval");
            }
        }

        private bool _useAppBar { get; set; }

        public bool UseAppBar
        {
            get
            {
                return _useAppBar;
            }
            set
            {
                _useAppBar = value;

                NotifyPropertyChanged("UseAppBar");
            }
        }

        private bool _alwaysTop { get; set; }

        public bool AlwaysTop
        {
            get
            {
                return _alwaysTop;
            }
            set
            {
                _alwaysTop = value;

                NotifyPropertyChanged("AlwaysTop");
            }
        }

        private bool _toolbarMode { get; set; }
        
        public bool ToolbarMode
        {
            get
            {
                return _toolbarMode;
            }
            set
            {
                _toolbarMode = value;

                NotifyPropertyChanged("ToolbarMode");
            }
        }

        private bool _clickThrough { get; set; }

        public bool ClickThrough
        {
            get
            {
                return _clickThrough;
            }
            set
            {
                _clickThrough = value;

                NotifyPropertyChanged("ClickThrough");
            }
        }

        private bool _showTrayIcon { get; set; }

        public bool ShowTrayIcon
        {
            get
            {
                return _showTrayIcon;
            }
            set
            {
                _showTrayIcon = value;

                NotifyPropertyChanged("ShowTrayIcon");
            }
        }

        private bool _runAtStartup { get; set; }

        public bool RunAtStartup
        {
            get
            {
                return _runAtStartup;
            }
            set
            {
                _runAtStartup = value;

                NotifyPropertyChanged("RunAtStartup");
            }
        }

        private int _sidebarWidth { get; set; }

        public int SidebarWidth
        {
            get
            {
                return _sidebarWidth;
            }
            set
            {
                _sidebarWidth = value;

                NotifyPropertyChanged("SidebarWidth");
            }
        }

        private bool _autoBGColor { get; set; }

        public bool AutoBGColor
        {
            get
            {
                return _autoBGColor;
            }
            set
            {
                _autoBGColor = value;

                NotifyPropertyChanged("AutoBGColor");
            }
        }

        private string _bgColor { get; set; }

        public string BGColor
        {
            get
            {
                return _bgColor;
            }
            set
            {
                _bgColor = value;

                NotifyPropertyChanged("BGColor");
            }
        }

        private double _bgOpacity { get; set; }

        public double BGOpacity
        {
            get
            {
                return _bgOpacity;
            }
            set
            {
                _bgOpacity = value;

                NotifyPropertyChanged("BGOpacity");
            }
        }

        private TextAlign _textAlign { get; set; }

        public TextAlign TextAlign
        {
            get
            {
                return _textAlign;
            }
            set
            {
                _textAlign = value;

                NotifyPropertyChanged("TextAlign");
            }
        }

        private TextAlignItem[] _textAlignItems { get; set; }

        public TextAlignItem[] TextAlignItems
        {
            get
            {
                return _textAlignItems;
            }
            set
            {
                _textAlignItems = value;

                NotifyPropertyChanged("TextAlignItems");
            }
        }

        private FontSetting _fontSetting { get; set; }

        public FontSetting FontSetting
        {
            get
            {
                return _fontSetting;
            }
            set
            {
                _fontSetting = value;

                NotifyPropertyChanged("FontSize");
            }
        }

        private FontSetting[] _fontSettingItems { get;  set;}

        public FontSetting[] FontSettingItems
        {
            get
            {
                return _fontSettingItems;
            }
            set
            {
                _fontSettingItems = value;

                NotifyPropertyChanged("FontSizeItems");
            }
        }
        private string _fontName { get; set; }

        public string FontName
        {
            get
            {
                return _fontName;
            }
            set
            {
                _fontName = value;

                NotifyPropertyChanged("FontName");
            }
        }

        private String[] _fontNameItems { get; set; }

        public String[] FontNameItems
        {
            get
            {
                return _fontNameItems;
            }
            set
            {
                _fontNameItems = value;

                NotifyPropertyChanged("FontNameItems");
            }
        }

        private string _fontColor { get; set; }

        public string FontColor
        {
            get
            {
                return _fontColor;
            }
            set
            {
                _fontColor = value;

                NotifyPropertyChanged("FontColor");
            }
        }

        private string _alertFontColor { get; set; }

        public string AlertFontColor
        {
            get
            {
                return _alertFontColor;
            }
            set
            {
                _alertFontColor = value;

                NotifyPropertyChanged("AlertFontColor");
            }
        }

        private bool _alertBlink { get; set; } = true;
        
        public bool AlertBlink
        {
            get
            {
                return _alertBlink;
            }
            set
            {
                _alertBlink = value;

                NotifyPropertyChanged("AlertBlink");
            }
        }

        private DateSetting _dateSetting { get; set; }

        public DateSetting DateSetting
        {
            get
            {
                return _dateSetting;
            }
            set
            {
                _dateSetting = value;

                NotifyPropertyChanged("DateSetting");
            }
        }

        private DateSetting[] _dateSettingItems { get; set; }

        public DateSetting[] DateSettingItems
        {
            get
            {
                return _dateSettingItems;
            }
            set
            {
                _dateSettingItems = value;

                NotifyPropertyChanged("DateSettingItems");
            }
        }

        private bool _collapseMenuBar { get; set; }

        public bool CollapseMenuBar
        {
            get
            {
                return _collapseMenuBar;
            }
            set
            {
                _collapseMenuBar = value;

                NotifyPropertyChanged("CollapseMenuBar");
            }
        }

        private bool _initiallyHidden { get; set; }
        
        public bool InitiallyHidden
        {
            get
            {
                return _initiallyHidden;
            }
            set
            {
                _initiallyHidden = value;

                NotifyPropertyChanged("InitiallyHidden");
            }
        }

        private string[] _iconThemeItems { get; set; }

        public string[] IconThemeItems
        {
            get
            {
                return _iconThemeItems;
            }
            set
            {
                _iconThemeItems = value;

                NotifyPropertyChanged("IconThemeItems");
            }
        }

        private string _iconTheme { get; set; } = "Default";

        public string IconTheme
        {
            get
            {
                return _iconTheme;
            }
            set
            {
                _iconTheme = value;

                NotifyPropertyChanged("IconTheme");
            }
        }

        private bool _showMachineName { get; set; } = true;

        public bool ShowMachineName
        {
            get
            {
                return _showMachineName;
            }
            set
            {
                _showMachineName = value;

                NotifyPropertyChanged("ShowMachineName");
            }
        }

        private bool _showClock { get; set; }

        public bool ShowClock
        {
            get
            {
                return _showClock;
            }
            set
            {
                _showClock = value;

                NotifyPropertyChanged("ShowClock");
            }
        }

        private bool _clock24HR { get; set; }

        public bool Clock24HR
        {
            get
            {
                return _clock24HR;
            }
            set
            {
                _clock24HR = value;

                NotifyPropertyChanged("Clock24HR");
            }
        }

        private ObservableCollection<MonitorConfig> _monitorConfig { get; set; }

        public ObservableCollection<MonitorConfig> MonitorConfig
        {
            get
            {
                return _monitorConfig;
            }
            set
            {
                _monitorConfig = value;

                _monitorConfig.CollectionChanged += Child_CollectionChanged;

                foreach (MonitorConfig _config in _monitorConfig)
                {
                    _config.PropertyChanged += Child_PropertyChanged;

                    _config.HardwareOC.CollectionChanged += Child_CollectionChanged;

                    foreach (HardwareConfig _hardware in _config.HardwareOC)
                    {
                        _hardware.PropertyChanged += Child_PropertyChanged;
                    }

                    foreach (MetricConfig _metric in _config.Metrics)
                    {
                        _metric.PropertyChanged += Child_PropertyChanged;
                    }

                    foreach (ConfigParam _param in _config.Params)
                    {
                        _param.PropertyChanged += Child_PropertyChanged;
                    }
                }

                NotifyPropertyChanged("MonitorConfig");
            }
        }

        private Hotkey _toggleKey { get; set; }

        public Hotkey ToggleKey
        {
            get
            {
                return _toggleKey;
            }
            set
            {
                _toggleKey = value;

                NotifyPropertyChanged("ToggleKey");
            }
        }

        private Hotkey _showKey { get; set; }

        public Hotkey ShowKey
        {
            get
            {
                return _showKey;
            }
            set
            {
                _showKey = value;

                NotifyPropertyChanged("ShowKey");
            }
        }

        private Hotkey _hideKey { get; set; }

        public Hotkey HideKey
        {
            get
            {
                return _hideKey;
            }
            set
            {
                _hideKey = value;

                NotifyPropertyChanged("HideKey");
            }
        }

        private Hotkey _reloadKey { get; set; }

        public Hotkey ReloadKey
        {
            get
            {
                return _reloadKey;
            }
            set
            {
                _reloadKey = value;

                NotifyPropertyChanged("ReloadKey");
            }
        }

        private Hotkey _closeKey { get; set; }

        public Hotkey CloseKey
        {
            get
            {
                return _closeKey;
            }
            set
            {
                _closeKey = value;

                NotifyPropertyChanged("CloseKey");
            }
        }

        private Hotkey _cycleEdgeKey { get; set; }

        public Hotkey CycleEdgeKey
        {
            get
            {
                return _cycleEdgeKey;
            }
            set
            {
                _cycleEdgeKey = value;

                NotifyPropertyChanged("CycleEdgeKey");
            }
        }

        private Hotkey _cycleScreenKey { get; set; }

        public Hotkey CycleScreenKey
        {
            get
            {
                return _cycleScreenKey;
            }
            set
            {
                _cycleScreenKey = value;

                NotifyPropertyChanged("CycleScreenKey");
            }
        }

        private Hotkey _reserveSpaceKey { get; set; }

        public Hotkey ReserveSpaceKey
        {
            get
            {
                return _reserveSpaceKey;
            }
            set
            {
                _reserveSpaceKey = value;

                NotifyPropertyChanged("ReserveSpaceKey");
            }
        }
    }

    public class DockItem
    {
        public DockEdge Value { get; set; }

        public string Text { get; set; }
    }

    public class ScreenItem
    {
        public int Index { get; set; }

        public string Text { get; set; }
    }

    public class TextAlignItem
    {
        public TextAlign Value { get; set; }

        public string Text { get; set; }
    }
}