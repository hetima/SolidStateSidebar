using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using SSS.Utilities;
using SSS.Core;
using SSS.Windows;
using System.Windows.Media;
using System.Drawing.Text;

namespace SSS.Models
{
    public class SettingsModel : ObservableObject
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
            FontNameItems = App.InitFontNameItems();
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
                        SSS.Module.ClaudeMonitor.Data => "ClaudeMonitor",
                        SSS.Module.CodexMonitor.Data => "CodexMonitor",
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

        public override void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            base.NotifyPropertyChanged(propertyName);

            if (propertyName != nameof(IsChanged))
            {
                IsChanged = true;
            }
        }

        private void Child_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            IsChanged = true;
        }

        private void Child_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            IsChanged = true;
        }

        private bool _isChanged = false;

        public bool IsChanged
        {
            get => _isChanged;
            set => SetProperty(ref _isChanged, value);
        }

        private DockEdge _dockEdge;

        public DockEdge DockEdge
        {
            get => _dockEdge;
            set => SetProperty(ref _dockEdge, value);
        }

        private DockItem[]? _dockEdgeItems;

        public DockItem[]? DockEdgeItems
        {
            get => _dockEdgeItems;
            set => SetProperty(ref _dockEdgeItems, value);
        }

        private int _screenIndex;

        public int ScreenIndex
        {
            get => _screenIndex;
            set => SetProperty(ref _screenIndex, value);
        }

        private ScreenItem[]? _screenItems;

        public ScreenItem[]? ScreenItems
        {
            get => _screenItems;
            set => SetProperty(ref _screenItems, value);
        }

        private string? _culture;

        public string Culture
        {
            get => _culture!;
            set => SetProperty(ref _culture, value);
        }

        private CultureItem[]? _cultureItems;

        public CultureItem[]? CultureItems
        {
            get => _cultureItems;
            set => SetProperty(ref _cultureItems, value);
        }

        private double _uiScale;

        public double UIScale
        {
            get => _uiScale;
            set => SetProperty(ref _uiScale, value);
        }

        private int _xOffset;

        public int XOffset
        {
            get => _xOffset;
            set => SetProperty(ref _xOffset, value);
        }

        private int _yOffset;

        public int YOffset
        {
            get => _yOffset;
            set => SetProperty(ref _yOffset, value);
        }

        private int _pollingInterval;

        public int PollingInterval
        {
            get => _pollingInterval;
            set => SetProperty(ref _pollingInterval, value);
        }

        private bool _useAppBar;

        public bool UseAppBar
        {
            get => _useAppBar;
            set => SetProperty(ref _useAppBar, value);
        }

        private bool _alwaysTop;

        public bool AlwaysTop
        {
            get => _alwaysTop;
            set => SetProperty(ref _alwaysTop, value);
        }

        private bool _toolbarMode;
        
        public bool ToolbarMode
        {
            get => _toolbarMode;
            set => SetProperty(ref _toolbarMode, value);
        }

        private bool _clickThrough;

        public bool ClickThrough
        {
            get => _clickThrough;
            set => SetProperty(ref _clickThrough, value);
        }

        private bool _showTrayIcon;

        public bool ShowTrayIcon
        {
            get => _showTrayIcon;
            set => SetProperty(ref _showTrayIcon, value);
        }

        private bool _runAtStartup;

        public bool RunAtStartup
        {
            get => _runAtStartup;
            set => SetProperty(ref _runAtStartup, value);
        }
        private int _sidebarMargin;

        public int SidebarMargin
        {
            get => _sidebarMargin;
            set => SetProperty(ref _sidebarMargin, value);
        }
        private int _sidebarWidth;

        public int SidebarWidth
        {
            get => _sidebarWidth;
            set => SetProperty(ref _sidebarWidth, value);
        }


        private string? _bgColor;

        public string BGColor
        {
            get => _bgColor!;
            set => SetProperty(ref _bgColor, value);
        }

        private double _bgOpacity;

        public double BGOpacity
        {
            get => _bgOpacity;
            set => SetProperty(ref _bgOpacity, value);
        }

        private TextAlign _textAlign;

        public TextAlign TextAlign
        {
            get => _textAlign;
            set => SetProperty(ref _textAlign, value);
        }

        private TextAlignItem[]? _textAlignItems;

        public TextAlignItem[] TextAlignItems
        {
            get => _textAlignItems!;
            set => SetProperty(ref _textAlignItems, value);
        }

        private int _fontSize;

        public int FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }
        private string? _fontName;

        public string FontName
        {
            get => _fontName!;
            set => SetProperty(ref _fontName, value);
        }

        private String[]? _fontNameItems;

        public String[] FontNameItems
        {
            get => _fontNameItems!;
            set => SetProperty(ref _fontNameItems, value);
        }

        private string? _fontColor;

        public string FontColor
        {
            get => _fontColor!;
            set => SetProperty(ref _fontColor, value);
        }

        private string? _alertFontColor;

        public string AlertFontColor
        {
            get => _alertFontColor!;
            set => SetProperty(ref _alertFontColor, value);
        }

        private bool _alertBlink = true;
        
        public bool AlertBlink
        {
            get => _alertBlink;
            set => SetProperty(ref _alertBlink, value);
        }

        private bool _collapseMenuBar;

        public bool CollapseMenuBar
        {
            get => _collapseMenuBar;
            set => SetProperty(ref _collapseMenuBar, value);
        }

        private bool _initiallyHidden;
        
        public bool InitiallyHidden
        {
            get => _initiallyHidden;
            set => SetProperty(ref _initiallyHidden, value);
        }

        private string[]? _iconThemeItems;

        public string[] IconThemeItems
        {
            get => _iconThemeItems!;
            set => SetProperty(ref _iconThemeItems, value);
        }

        private string? _iconTheme = "Default";

        public string IconTheme
        {
            get => _iconTheme!;
            set => SetProperty(ref _iconTheme, value);
        }

        private bool _showMachineName = true;

        public bool ShowMachineName
        {
            get => _showMachineName;
            set => SetProperty(ref _showMachineName, value);
        }

        private bool _metricsNoWrap = false;

        public bool MetricsNoWrap
        {
            get => _metricsNoWrap;
            set => SetProperty(ref _metricsNoWrap, value);
        }

        private ObservableCollection<IModuleData>? _modules;

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

        private IModuleData? _selectedModule;

        public IModuleData? SelectedModule
        {
            get => _selectedModule;
            set => SetProperty(ref _selectedModule, value);
        }

        private Hotkey? _toggleKey;

        public Hotkey? ToggleKey
        {
            get => _toggleKey;
            set => SetProperty(ref _toggleKey, value);
        }

        private Hotkey? _showKey;

        public Hotkey? ShowKey
        {
            get => _showKey;
            set => SetProperty(ref _showKey, value);
        }

        private Hotkey? _hideKey;

        public Hotkey? HideKey
        {
            get => _hideKey;
            set => SetProperty(ref _hideKey, value);
        }

        private Hotkey? _reloadKey;

        public Hotkey? ReloadKey
        {
            get => _reloadKey;
            set => SetProperty(ref _reloadKey, value);
        }

        private Hotkey? _closeKey;

        public Hotkey? CloseKey
        {
            get => _closeKey;
            set => SetProperty(ref _closeKey, value);
        }

        private Hotkey? _cycleEdgeKey;

        public Hotkey? CycleEdgeKey
        {
            get => _cycleEdgeKey;
            set => SetProperty(ref _cycleEdgeKey, value);
        }

        private Hotkey? _cycleScreenKey = null;

        public Hotkey? CycleScreenKey
        {
            get => _cycleScreenKey;
            set => SetProperty(ref _cycleScreenKey, value);
        }

        private Hotkey? _reserveSpaceKey;

        public Hotkey? ReserveSpaceKey
        {
            get => _reserveSpaceKey;
            set => SetProperty(ref _reserveSpaceKey, value);
        }
    }

}