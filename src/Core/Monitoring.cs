// This file has been split into multiple files for better organization:
// - MonitoringEnums.cs   (MonitorType, MetricKey, DataType)
// - Converters.cs        (iConverter, CelciusToFahrenheit, MHzToGHz, BitsPerSecondConverter, BytesPerSecondConverter)
// - MonitorBase.cs       (iMonitor, BaseMonitor)
// - MetricBase.cs        (iMetric, BaseMetric)
// - OHMMonitorBase.cs    (OHMMonitorBase abstract, shared sensor helpers)
// - MetricTypes.cs       (OHMMetric, GPUVRAMMLoadMetric, IPMetric, PCMetric)
// - HardwareConfig.cs    (HardwareConfig)
// - MetricConfig.cs      (MetricConfig)
// - IModuleData.cs       (IModuleData)
// - ModuleDataConverter.cs (ModuleDataConverter)
// - MonitorPanel.cs      (MonitorPanel)
// - MonitorManager.cs    (MonitorManager)
// - MonitoringExtensions.cs (Extensions)
//
//   Module-specific monitors (in their respective module folders):
// - [Modules/CpuMonitor/CPUMonitor.cs]          (CPUMonitor)
// - [Modules/RamMonitor/RAMMonitor.cs]          (RAMMonitor)
// - [Modules/GpuMonitor/GPUMonitor.cs]          (GPUMonitor)
// - [Modules/HdMonitor/DriveMonitor.cs]         (DriveMonitor)
// - [Modules/TimeMonitor/ClockMonitor.cs]       (ClockMonitor)
// - [Modules/NetworkMonitor/NetworkMonitor.cs]  (NetworkMonitor)
// - [Modules/WindowMonitor/WindowMonitor.cs]    (WindowMonitor)
// - [Modules/WindowMonitor/WindowMonitor.cs] (WindowMonitor)

namespace SSS.Core
{
}
