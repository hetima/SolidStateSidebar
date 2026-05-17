using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using LibreHardwareMonitor.Hardware;
using CpuData = SSS.Module.CpuMonitor.Data;
using GpuData = SSS.Module.GpuMonitor.Data;
using HdData = SSS.Module.HdMonitor.Data;
using NetData = SSS.Module.NetworkMonitor.Data;
using RamData = SSS.Module.RamMonitor.Data;
using TimeData = SSS.Module.TimeMonitor.Data;

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

            var ramData = modules["RamMonitor"];
            // De-duplicate RAM hardware IDs
            if (ramData.Hardware != null)
            {
                ramData.Hardware = ramData.Hardware
                    .GroupBy(h => h.ID)
                    .Select(g => g.First())
                    .ToArray();
            }

            MonitorPanels = modules
                .Where(m => m.Value.Enabled)
                .OrderByDescending(m => m.Value.Order)
                .Select(m => m.Key switch
                {
                    "CpuMonitor" => CpuPanel(m.Value),
                    "RamMonitor" => RamPanel(m.Value),
                    "GpuMonitor" => GpuPanel(m.Value),
                    "HdMonitor" => HdPanel(m.Value),
                    "NetworkMonitor" => NetworkPanel(m.Value),
                    "TimeMonitor" => TimePanel(m.Value),
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
                    return DriveMonitor.GetHardware().ToArray();

                case MonitorType.Network:
                    return NetworkMonitor.GetHardware().ToArray();

                case MonitorType.Time:
                    return ClockMonitor.GetHardware().ToArray();

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

        private MonitorPanel CpuPanel(IModuleData data)
        {
            var d = (CpuData)data;
            return new MonitorPanel(
                MonitorType.CPU,
                MonitorType.CPU.GetDescription(),
                Core.Settings.Instance.GetIconSvgPath("cpu"),
                OHMMonitor.GetInstances(d.Hardware!, d.Metrics!, MonitorType.CPU, _board!, GetHardware(MonitorType.CPU.GetHardwareTypes()).ToArray(),
                    d.ShowHardwareNames, d.RoundAll, d.AllCoreClocks, d.UseGHz, d.UseFahrenheit, d.TempAlert)
                );
        }

        private MonitorPanel RamPanel(IModuleData data)
        {
            var d = (RamData)data;
            return new MonitorPanel(
                MonitorType.RAM,
                MonitorType.RAM.GetDescription(),
                Core.Settings.Instance.GetIconSvgPath("ram"),
                OHMMonitor.GetInstances(d.Hardware!, d.Metrics!, MonitorType.RAM, _board!, GetHardware(MonitorType.RAM.GetHardwareTypes()).ToArray(),
                    false, d.RoundAll, false, false, false, 0)
                );
        }

        private MonitorPanel GpuPanel(IModuleData data)
        {
            var d = (GpuData)data;
            return new MonitorPanel(
                MonitorType.GPU,
                MonitorType.GPU.GetDescription(),
                Core.Settings.Instance.GetIconSvgPath("gpu"),
                OHMMonitor.GetInstances(d.Hardware!, d.Metrics!, MonitorType.GPU, _board!, GetHardware(MonitorType.GPU.GetHardwareTypes()).ToArray(),
                    d.ShowHardwareNames, d.RoundAll, false, d.UseGHz, d.UseFahrenheit, d.TempAlert)
                );
        }

        private MonitorPanel HdPanel(IModuleData data)
        {
            var d = (HdData)data;
            return new MonitorPanel(
                MonitorType.HD,
                MonitorType.HD.GetDescription(),
                Core.Settings.Instance.GetIconSvgPath("hd"),
                DriveMonitor.GetInstances(d.Hardware!, d.Metrics!, d.RoundAll, d.UsedSpaceAlert)
                );
        }

        private MonitorPanel NetworkPanel(IModuleData data)
        {
            var d = (NetData)data;
            return new MonitorPanel(
                MonitorType.Network,
                MonitorType.Network.GetDescription(),
                Core.Settings.Instance.GetIconSvgPath("net"),
                NetworkMonitor.GetInstances(d.Hardware!, d.Metrics!, d.ShowHardwareNames, d.RoundAll, d.UseBytes, d.BandwidthInAlert, d.BandwidthOutAlert)
                );
        }

        private MonitorPanel TimePanel(IModuleData data)
        {
            var d = (TimeData)data;
            return new MonitorPanel(
                MonitorType.Time,
                MonitorType.Time.GetDescription(),
                Core.Settings.Instance.GetIconSvgPath("clock"),
                ClockMonitor.GetInstances(d.Hardware!, d.Metrics!, d.Clock24HR, d.DateFormat)
                );
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
