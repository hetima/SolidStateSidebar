using System.Windows;
using System.Windows.Controls;
using CpuData = SSS.Module.CpuMonitor.Data;
using RamData = SSS.Module.RamMonitor.Data;
using GpuData = SSS.Module.GpuMonitor.Data;
using HdData = SSS.Module.HdMonitor.Data;
using NetData = SSS.Module.NetworkMonitor.Data;
using TimeData = SSS.Module.TimeMonitor.Data;
using WindowData = SSS.Module.WindowMonitor.Data;
using ClaudeData = SSS.Module.ClaudeMonitor.Data;
using CodexData = SSS.Module.CodexMonitor.Data;

namespace SSS.Converters
{
    public class MonitorConfigTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? CpuTemplate { get; set; }

        public DataTemplate? RamTemplate { get; set; }

        public DataTemplate? GpuTemplate { get; set; }

        public DataTemplate? HdTemplate { get; set; }

        public DataTemplate? NetworkTemplate { get; set; }

        public DataTemplate? TimeTemplate { get; set; }

        public DataTemplate? WindowTemplate { get; set; }

        public DataTemplate? ClaudeTemplate { get; set; }

        public DataTemplate? CodexTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            return item switch
            {
                CpuData => CpuTemplate,
                RamData => RamTemplate,
                GpuData => GpuTemplate,
                HdData => HdTemplate,
                NetData => NetworkTemplate,
                TimeData => TimeTemplate,
                WindowData => WindowTemplate,
                ClaudeData => ClaudeTemplate,
                CodexData => CodexTemplate,
                _ => base.SelectTemplate(item, container)
            };
        }
    }
}
