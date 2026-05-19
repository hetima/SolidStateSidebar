using System.Windows;
using System.Windows.Controls;

namespace SSS.Converters
{
    public class MonitorPanelTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? CpuTemplate { get; set; }

        public DataTemplate? RamTemplate { get; set; }

        public DataTemplate? GpuTemplate { get; set; }

        public DataTemplate? HdTemplate { get; set; }

        public DataTemplate? NetworkTemplate { get; set; }

        public DataTemplate? TimeTemplate { get; set; }

        public DataTemplate? WindowTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is Core.MonitorPanel panel)
            {
                return panel.Type switch
                {
                    Core.MonitorType.CPU => CpuTemplate,
                    Core.MonitorType.RAM => RamTemplate,
                    Core.MonitorType.GPU => GpuTemplate,
                    Core.MonitorType.HD => HdTemplate,
                    Core.MonitorType.Network => NetworkTemplate,
                    Core.MonitorType.Time => TimeTemplate,
                    Core.MonitorType.Window => WindowTemplate,
                    _ => null
                };
            }

            return base.SelectTemplate(item, container);
        }
    }
}
