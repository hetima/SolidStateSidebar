using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using SSS.Utilities;
using SSS.Core;
using SSS.Windows;
using System.Windows.Media;
using System.Drawing.Text;

namespace SSS.Models
{
    public class SettingsModel : INotifyPropertyChanged
    {
        public SettingsModel(Sidebar sidebar)
        {
            DockEdgeItems =
            [
                new DockItem() { Text = Strings.SettingsDockLeft, Value = DockEdge.Left },
                new DockItem() { Text = Strings.SettingsDockRight, Value = DockEdge.Right }
            ];

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
            SidebarMargin = Core.Settings.Instance.SidebarMargin;
            BGColor = Core.Settings.Instance.BGColor;
            BGOpacity = Core.Settings.Instance.BGOpacity;

            TextAlignItems =
            [
                new TextAlignItem() { Text = Strings.SettingsTextAlignLeft, Value = TextAlign.Left },
                new TextAlignItem() { Text = Strings.SettingsTextAlignRight, Value = TextAlign.Right }
            ];

            TextAlign = Core.Settings.Instance.TextAlign;

            FontSize = Core.Settings.Instance.FontSize;
            FontNameItems = Fonts.SystemFontFamilies.Select(i => i.Source).ToArray();
            FontName = Core.Settings.Instance.FontName;
            FontColor = Core.Settings.Instance.FontColor;
            AlertFontColor = Core.Settings.Instance.AlertFontColor;
            AlertBlink = Core.Settings.Instance.AlertBlink;
            IconThemeItems = Core.Settings.IconThemeList;
            IconTheme = Core.Settings.Instance.IconTheme;

            CollapseMenuBar = Core.Settings.Instance.CollapseMenuBar;
            InitiallyHidden = Core.Settings.Instance.InitiallyHidden;
            ShowMachineName = Core.Settings.Instance.ShowMachineName;
            MetricsNoWrap = Core.Settings.Instance.MetricsNoWrap;

            ObservableCollection<IModuleData> _modules = new ObservableCollection<IModuleData>(
                from kvp in Core.Settings.Instance.Modules!
                orderby kvp.Value.Order descending
                select kvp.Value.Clone()
                );

            if (sidebar != null && sidebar.Ready)
            {
                MonitorType[] ohmTypes = [MonitorType.CPU, MonitorType.RAM, MonitorType.GPU];

                foreach (IModuleData _module in _modules)
                {
                    MonitorType? _type = _module switch
                    {
                        SSS.Module.CpuMonitor.Data => MonitorType.CPU,
                        SSS.Module.RamMonitor.Data => MonitorType.RAM,
                        SSS.Module.GpuMonitor.Data => MonitorType.GPU,
                        SSS.Module.HdMonitor.Data => MonitorType.HD,
                        SSS.Module.NetworkMonitor.Data => MonitorType.Network,
                        SSS.Module.TimeMonitor.Data => MonitorType.Time,
                        SSS.Module.WindowMonitor.Data => MonitorType.Window,
                        _ => null
                    };

                    if (_type == null || _module.Hardware == null)
                        continue;

                    _module.HardwareOC = new ObservableCollection<HardwareConfig>(
                        from hw in sidebar.Model!.MonitorManager.GetHardware(_type.Value)
                        join config in _module.Hardware on hw.ID equals config.ID into merged
                        from newhw in merged.DefaultIfEmpty(hw).Select(newhw => { newhw.ActualName = hw.ActualName; if (string.IsNullOrEmpty(newhw.Name)) { newhw.Name = hw.ActualName; } return newhw; })
                        orderby newhw.Order descending, newhw.Name ascending
                        select newhw
                        );

                    // WindowMonitor: initialize ApplicationOC
                    if (_module is SSS.Module.WindowMonitor.Data wmData && wmData.Applications != null)
                    {
                        wmData.ApplicationOC = new ObservableCollection<HardwareConfig>(
                            from app in wmData.Applications
                            let displayName = !string.IsNullOrEmpty(app.Name) ? app.Name : (app.ActualName ?? app.ID ?? "")
                            orderby app.Order descending, displayName ascending
                            select new HardwareConfig
                            {
                                ID = app.ID,
                                Name = displayName,
                                ActualName = app.ActualName,
                                Enabled = app.Enabled,
                                Order = app.Order
                            }
                            );
                    }
                }
            }

            foreach (IModuleData _module in _modules)
            {
                if (_module.Metrics != null)
                {
                    var _sorted = _module.Metrics.OrderByDescending(m => m.Order).ThenBy(m => m.Name).ToArray();

                    _module.Metrics.Clear();

                    foreach (var m in _sorted)
                    {
                        _module.Metrics.Add(m);
                    }
                }
            }

            Modules = _modules;

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
            Core.Settings.Instance.SidebarMargin = SidebarMargin;
            Core.Settings.Instance.BGColor = BGColor;
            Core.Settings.Instance.BGOpacity = BGOpacity;
            Core.Settings.Instance.TextAlign = TextAlign;
            Core.Settings.Instance.FontSize = FontSize;
            Core.Settings.Instance.FontName = FontName;
            Core.Settings.Instance.FontColor = FontColor;
            Core.Settings.Instance.AlertFontColor = AlertFontColor;
            Core.Settings.Instance.AlertBlink = AlertBlink;
            Core.Settings.Instance.IconTheme = IconTheme;
            Core.Settings.Instance.CollapseMenuBar = CollapseMenuBar;
            Core.Settings.Instance.InitiallyHidden = InitiallyHidden;
            Core.Settings.Instance.ShowMachineName = ShowMachineName;
            Core.Settings.Instance.MetricsNoWrap = MetricsNoWrap;

            for (int i = 0; i < Modules.Count; i++)
            {
                IModuleData _module = Modules[i];

                if (_module.HardwareOC != null)
                {
                    HardwareConfig[] _hardware = new HardwareConfig[_module.HardwareOC.Count];

                    for (int v = 0; v < _hardware.Length; v++)
                    {
                        _hardware[v] = _module.HardwareOC[v].Clone();

                        _hardware[v].Order = Convert.ToByte(_hardware.Length - v);

                        if (string.IsNullOrEmpty(_hardware[v].Name) || string.Equals(_hardware[v].Name, _hardware[v].ActualName, StringComparison.Ordinal))
                        {
                            _hardware[v].Name = null;
                        }
                    }

                    _module.Hardware = _hardware;
                }

                if (_module is SSS.Module.WindowMonitor.Data wmData && wmData.ApplicationOC != null)
                {
                    HardwareConfig[] _applications = new HardwareConfig[wmData.ApplicationOC.Count];

                    for (int v = 0; v < _applications.Length; v++)
                    {
                        _applications[v] = wmData.ApplicationOC[v].Clone();

                        _applications[v].Order = Convert.ToByte(_applications.Length - v);
                    }

                    wmData.Applications = _applications;
                }

                if (_module.Metrics != null)
                {
                    for (int j = 0; j < _module.Metrics.Count; j++)
                    {
                        _module.Metrics[j].Order = Convert.ToByte(_module.Metrics.Count - j);
                    }
                }

                _module.Order = Convert.ToByte(Modules.Count - i);
            }

            Core.Settings.Instance.Modules = new Dictionary<string, IModuleData>(
                Modules.Select(m => new KeyValuePair<string, IModuleData>(
                    m switch
                    {
                        SSS.Module.CpuMonitor.Data => "CpuMonitor",
                        SSS.Module.RamMonitor.Data => "RamMonitor",
                        SSS.Module.GpuMonitor.Data => "GpuMonitor",
                        SSS.Module.HdMonitor.Data => "HdMonitor",
                        SSS.Module.NetworkMonitor.Data => "NetworkMonitor",
                        SSS.Module.TimeMonitor.Data => "TimeMonitor",
                        SSS.Module.WindowMonitor.Data => "WindowMonitor",
                        _ => throw new InvalidOperationException("Unknown module data type")
                    }, m
                ))
                );

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

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Child_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            IsChanged = true;
        }

        private void Child_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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

                NotifyPropertyChanged(nameof(IsChanged));
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

                NotifyPropertyChanged(nameof(DockEdge));
            }
        }

        private DockItem[]? _dockEdgeItems { get; set; }

        public DockItem[]? DockEdgeItems
        {
            get
            {
                return _dockEdgeItems;
            }
            set
            {
                _dockEdgeItems = value;

                NotifyPropertyChanged(nameof(DockEdgeItems));
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

                NotifyPropertyChanged(nameof(ScreenIndex));
            }
        }

        private ScreenItem[]? _screenItems { get; set; }

        public ScreenItem[]? ScreenItems
        {
            get
            {
                return _screenItems;
            }
            set
            {
                _screenItems = value;

                NotifyPropertyChanged(nameof(ScreenItems));
            }
        }

        private string? _culture { get; set; }

        public string Culture
        {
            get
            {
                return _culture!;
            }
            set
            {
                _culture = value;

                NotifyPropertyChanged(nameof(Culture));
            }
        }

        private CultureItem[]? _cultureItems { get; set; }

        public CultureItem[]? CultureItems
        {
            get
            {
                return _cultureItems;
            }
            set
            {
                _cultureItems = value;

                NotifyPropertyChanged(nameof(CultureItems));
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

                NotifyPropertyChanged(nameof(UIScale));
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

                NotifyPropertyChanged(nameof(XOffset));
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

                NotifyPropertyChanged(nameof(YOffset));
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

                NotifyPropertyChanged(nameof(PollingInterval));
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

                NotifyPropertyChanged(nameof(UseAppBar));
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

                NotifyPropertyChanged(nameof(AlwaysTop));
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

                NotifyPropertyChanged(nameof(ToolbarMode));
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

                NotifyPropertyChanged(nameof(ClickThrough));
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

                NotifyPropertyChanged(nameof(ShowTrayIcon));
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

                NotifyPropertyChanged(nameof(RunAtStartup));
            }
        }
        private int _sidebarMargin { get; set; }

        public int SidebarMargin
        {
            get
            {
                return _sidebarMargin;
            }
            set
            {
                _sidebarMargin = value;

                NotifyPropertyChanged(nameof(SidebarMargin));
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

                NotifyPropertyChanged(nameof(SidebarWidth));
            }
        }


        private string? _bgColor { get; set; }

        public string BGColor
        {
            get
            {
                return _bgColor!;
            }
            set
            {
                _bgColor = value;

                NotifyPropertyChanged(nameof(BGColor));
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

                NotifyPropertyChanged(nameof(BGOpacity));
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

                NotifyPropertyChanged(nameof(TextAlign));
            }
        }

        private TextAlignItem[]? _textAlignItems { get; set; }

        public TextAlignItem[] TextAlignItems
        {
            get
            {
                return _textAlignItems!;
            }
            set
            {
                _textAlignItems = value;

                NotifyPropertyChanged(nameof(TextAlignItems));
            }
        }

        private int _fontSize { get; set; }

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
            }
        }
        private string? _fontName { get; set; }

        public string FontName
        {
            get
            {
                return _fontName!;
            }
            set
            {
                _fontName = value;

                NotifyPropertyChanged(nameof(FontName));
            }
        }

        private String[]? _fontNameItems { get; set; }

        public String[] FontNameItems
        {
            get
            {
                return _fontNameItems!;
            }
            set
            {
                _fontNameItems = value;

                NotifyPropertyChanged(nameof(FontNameItems));
            }
        }

        private string? _fontColor { get; set; }

        public string FontColor
        {
            get
            {
                return _fontColor!;
            }
            set
            {
                _fontColor = value;

                NotifyPropertyChanged(nameof(FontColor));
            }
        }

        private string? _alertFontColor { get; set; }

        public string AlertFontColor
        {
            get
            {
                return _alertFontColor!;
            }
            set
            {
                _alertFontColor = value;

                NotifyPropertyChanged(nameof(AlertFontColor));
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

                NotifyPropertyChanged(nameof(AlertBlink));
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

                NotifyPropertyChanged(nameof(CollapseMenuBar));
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

                NotifyPropertyChanged(nameof(InitiallyHidden));
            }
        }

        private string[]? _iconThemeItems { get; set; }

        public string[] IconThemeItems
        {
            get
            {
                return _iconThemeItems!;
            }
            set
            {
                _iconThemeItems = value;

                NotifyPropertyChanged(nameof(IconThemeItems));
            }
        }

        private string? _iconTheme { get; set; } = "Default";

        public string IconTheme
        {
            get
            {
                return _iconTheme!;
            }
            set
            {
                _iconTheme = value;

                NotifyPropertyChanged(nameof(IconTheme));
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

                NotifyPropertyChanged(nameof(ShowMachineName));
            }
        }

        private bool _metricsNoWrap { get; set; } = false;

        public bool MetricsNoWrap
        {
            get
            {
                return _metricsNoWrap;
            }
            set
            {
                _metricsNoWrap = value;

                NotifyPropertyChanged(nameof(MetricsNoWrap));
            }
        }

        private ObservableCollection<IModuleData>? _modules { get; set; }

        public ObservableCollection<IModuleData> Modules
        {
            get
            {
                return _modules!;
            }
            set
            {
                _modules = value;

                _modules.CollectionChanged += Child_CollectionChanged;

                foreach (IModuleData _module in _modules)
                {
                    _module.PropertyChanged += Child_PropertyChanged;

                    if (_module.HardwareOC != null)
                    {
                        _module.HardwareOC.CollectionChanged += Child_CollectionChanged;

                        foreach (HardwareConfig _hardware in _module.HardwareOC)
                        {
                            _hardware.PropertyChanged += Child_PropertyChanged;
                        }
                    }

                    if (_module.Metrics != null)
                    {
                        foreach (MetricConfig _metric in _module.Metrics)
                        {
                            _metric.PropertyChanged += Child_PropertyChanged;
                        }
                    }
                }

                SelectedModule = _modules.FirstOrDefault();

                NotifyPropertyChanged(nameof(Modules));
            }
        }

        private IModuleData? _selectedModule { get; set; }

        public IModuleData? SelectedModule
        {
            get
            {
                return _selectedModule;
            }
            set
            {
                _selectedModule = value;

                NotifyPropertyChanged(nameof(SelectedModule));
            }
        }

        private Hotkey? _toggleKey { get; set; }

        public Hotkey? ToggleKey
        {
            get
            {
                return _toggleKey;
            }
            set
            {
                _toggleKey = value;

                NotifyPropertyChanged(nameof(ToggleKey));
            }
        }

        private Hotkey? _showKey { get; set; }

        public Hotkey? ShowKey
        {
            get
            {
                return _showKey;
            }
            set
            {
                _showKey = value;

                NotifyPropertyChanged(nameof(ShowKey));
            }
        }

        private Hotkey? _hideKey { get; set; }

        public Hotkey? HideKey
        {
            get
            {
                return _hideKey;
            }
            set
            {
                _hideKey = value;

                NotifyPropertyChanged(nameof(HideKey));
            }
        }

        private Hotkey? _reloadKey { get; set; }

        public Hotkey? ReloadKey
        {
            get => _reloadKey;
            set
            {
                _reloadKey = value;

                NotifyPropertyChanged(nameof(ReloadKey));
            }
        }

        private Hotkey? _closeKey { get; set; }

        public Hotkey? CloseKey
        {
            get => _closeKey;
            set
            {
                _closeKey = value;

                NotifyPropertyChanged(nameof(CloseKey));
            }
        }

        private Hotkey? _cycleEdgeKey { get; set; }

        public Hotkey? CycleEdgeKey
        {
            get => _cycleEdgeKey;
            set
            {
                _cycleEdgeKey = value;

                NotifyPropertyChanged(nameof(CycleEdgeKey));
            }
        }

        private Hotkey? _cycleScreenKey { get; set; } = null;

        public Hotkey? CycleScreenKey
        {
            get => _cycleScreenKey;
            set
            {
                _cycleScreenKey = value;

                NotifyPropertyChanged(nameof(CycleScreenKey));
            }
        }

        private Hotkey? _reserveSpaceKey { get; set; }

        public Hotkey? ReserveSpaceKey
        {
            get
            {
                return _reserveSpaceKey;
            }
            set
            {
                _reserveSpaceKey = value;

                NotifyPropertyChanged(nameof(ReserveSpaceKey));
            }
        }
    }

    public class DockItem
    {
        public DockEdge Value { get; set; }

        public string? Text { get; set; }
    }

    public class ScreenItem
    {
        public int Index { get; set; }

        public string? Text { get; set; }
    }

    public class TextAlignItem
    {
        public TextAlign Value { get; set; }

        public string? Text { get; set; }
    }
}