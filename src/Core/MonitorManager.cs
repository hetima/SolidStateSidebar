using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using LibreHardwareMonitor.Hardware;
using HdData = SSS.Module.HdMonitor.Data;
using NetData = SSS.Module.NetworkMonitor.Data;
using TimeData = SSS.Module.TimeMonitor.Data;
using WindowData = SSS.Module.WindowMonitor.Data;

namespace SSS.Core
{
    public class MonitorManager : INotifyPropertyChanged, IDisposable
    {

        public MonitorManager(Dictionary<string, IModuleData> modules)
        {
            _computer = new Computer()
            {
                IsCpuEnabled = true,
                IsControllerEnabled = true,
                IsGpuEnabled = true,
                IsStorageEnabled = false,
                IsMotherboardEnabled = true,
                IsMemoryEnabled = true,
                IsNetworkEnabled = false
            };
            _computer.Open();
            _board = GetHardware(HardwareType.Motherboard).FirstOrDefault();

            UpdateBoard();

            MonitorPanels = modules
                .Where(m => m.Value.Enabled)
                .OrderByDescending(m => m.Value.Order)
                .Select(m => m.Key switch
                {
                    "CpuMonitor" => CreateCpuPanel(m.Value),
                    "RamMonitor" => CreateRamPanel(m.Value),
                    "GpuMonitor" => CreateGpuPanel(m.Value),
                    "HdMonitor" => HdPanel(m.Value),
                    "NetworkMonitor" => NetworkPanel(m.Value),
                    "TimeMonitor" => TimePanel(m.Value),
                    "WindowMonitor" => WindowPanel(m.Value),
                    _ => null
                })
                .Where(p => p != null)
                .ToArray()!;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (MonitorPanels != null)
                    {
                        foreach (MonitorPanel _panel in MonitorPanels)
                        {
                            _panel.Dispose();
                        }
                    }

                    _computer?.Close();

                    _monitorPanels = null;
                    _computer = null;
                    _board = null;
                }

                _disposed = true;
            }
        }

        ~MonitorManager()
        {
            Dispose(false);
        }

        public HardwareConfig[] GetHardware(MonitorType type)
        {
            switch (type)
            {
                case MonitorType.CPU:
                case MonitorType.RAM:
                case MonitorType.GPU:
                    return GetHardware(type.GetHardwareTypes()).Select(h => new HardwareConfig() { ID = h.Identifier.ToString(), Name = h.Name, ActualName = h.Name }).ToArray();

                case MonitorType.HD:
                    return SSS.Module.HdMonitor.DriveMonitor.GetHardware().ToArray();

                case MonitorType.Network:
                    return SSS.Module.NetworkMonitor.NetworkMonitor.GetHardware().ToArray();

                case MonitorType.Time:
                    return SSS.Module.TimeMonitor.ClockMonitor.GetHardware().ToArray();

                case MonitorType.Window:
                    return [];

                default:
                    throw new ArgumentException("Invalid MonitorType.");
            }
        }

        public void Update()
        {
            UpdateBoard();

            foreach (iMonitor _monitor in MonitorPanels!.SelectMany(p => p.Monitors))
            {
                _monitor.Update();
            }
        }

        public void UpdateTime()
        {
            if (MonitorPanels == null) return;

            foreach (MonitorPanel _panel in MonitorPanels.Where(p => p.Type == MonitorType.Time))
            {
                foreach (iMonitor _monitor in _panel.Monitors)
                {
                    _monitor.Update();
                }
            }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private IEnumerable<IHardware> GetHardware(params HardwareType[] types)
        {
            return _computer!.Hardware.Where(h => types.Contains(h.HardwareType));
        }

        private MonitorPanel CreateCpuPanel(IModuleData data)
        {
            var d = (SSS.Module.CpuMonitor.Data)data;
            var hw = GetHardware(MonitorType.CPU.GetHardwareTypes()).ToArray();
            var panel = new MonitorPanel(
                MonitorType.CPU,
                MonitorType.CPU.GetDescription(),
                Core.Settings.Instance.GetIconSvgPath("cpu"),
                SSS.Module.CpuMonitor.CPUMonitor.GetInstances(d, _board!, hw)
                );
            panel.SectionHeaderStyle = d.SectionHeaderStyle;
            return panel;
        }

        private MonitorPanel CreateRamPanel(IModuleData data)
        {
            var d = (SSS.Module.RamMonitor.Data)data;
            var hw = GetHardware(MonitorType.RAM.GetHardwareTypes()).ToArray();
            var panel = new MonitorPanel(
                MonitorType.RAM,
                MonitorType.RAM.GetDescription(),
                Core.Settings.Instance.GetIconSvgPath("ram"),
                SSS.Module.RamMonitor.RAMMonitor.GetInstances(d, _board!, hw)
                );
            panel.SectionHeaderStyle = d.SectionHeaderStyle;
            return panel;
        }

        private MonitorPanel CreateGpuPanel(IModuleData data)
        {
            var d = (SSS.Module.GpuMonitor.Data)data;
            var hw = GetHardware(MonitorType.GPU.GetHardwareTypes()).ToArray();
            var panel = new MonitorPanel(
                MonitorType.GPU,
                MonitorType.GPU.GetDescription(),
                Core.Settings.Instance.GetIconSvgPath("gpu"),
                SSS.Module.GpuMonitor.GPUMonitor.GetInstances(d, _board!, hw)
                );
            panel.SectionHeaderStyle = d.SectionHeaderStyle;
            return panel;
        }

        private MonitorPanel HdPanel(IModuleData data)
        {
            var d = (HdData)data;
            var panel = new MonitorPanel(
                MonitorType.HD,
                MonitorType.HD.GetDescription(),
                Core.Settings.Instance.GetIconSvgPath("hd"),
                SSS.Module.HdMonitor.DriveMonitor.GetInstances(d.Hardware!, d.Metrics!.ToArray(), d.RoundAll, d.UsedSpaceAlert)
                );
            panel.SectionHeaderStyle = d.SectionHeaderStyle;
            return panel;
        }

        private MonitorPanel NetworkPanel(IModuleData data)
        {
            var d = (NetData)data;
            var panel = new MonitorPanel(
                MonitorType.Network,
                MonitorType.Network.GetDescription(),
                Core.Settings.Instance.GetIconSvgPath("net"),
                SSS.Module.NetworkMonitor.NetworkMonitor.GetInstances(d.Hardware!, d.Metrics!.ToArray(), d.ShowHardwareNames, d.RoundAll, d.UseBytes, d.BandwidthInAlert, d.BandwidthOutAlert)
                );
            panel.SectionHeaderStyle = d.SectionHeaderStyle;
            return panel;
        }

        private MonitorPanel TimePanel(IModuleData data)
        {
            var d = (TimeData)data;
            var panel = new MonitorPanel(
                MonitorType.Time,
                MonitorType.Time.GetDescription(),
                Core.Settings.Instance.GetIconSvgPath("clock"),
                SSS.Module.TimeMonitor.ClockMonitor.GetInstances(d.Hardware!, d.ShowDate, d.ShowTime, d.Clock24HR, d.DateFormat, d.ShowDayOfWeek, d.DateFontSize, d.TimeFontSize)
                );
            panel.SectionHeaderStyle = d.SectionHeaderStyle;
            return panel;
        }

        private MonitorPanel WindowPanel(IModuleData data)
        {
            var d = (WindowData)data;
            var panel = new MonitorPanel(
                MonitorType.Window,
                MonitorType.Window.GetDescription(),
                Core.Settings.Instance.GetIconSvgPath("win"),
                SSS.Module.WindowMonitor.WindowMonitor.GetInstances(d.Applications ?? [], d.MaxDisplayCount)
                );
            panel.SectionHeaderStyle = d.SectionHeaderStyle;
            return panel;
        }

        private void UpdateBoard()
        {
            _board?.Update();
        }

        private MonitorPanel[]? _monitorPanels { get; set; }

        public MonitorPanel[]? MonitorPanels
        {
            get
            {
                return _monitorPanels;
            }
            private set
            {
                _monitorPanels = value;

                NotifyPropertyChanged(nameof(MonitorPanels));
            }
        }

        private Computer? _computer { get; set; }

        private IHardware? _board { get; set; }

        private bool _disposed { get; set; } = false;
    }
}
