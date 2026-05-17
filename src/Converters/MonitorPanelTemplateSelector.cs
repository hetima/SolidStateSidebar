using System.Windows;
using System.Windows.Controls;

namespace SSS.Converters
{
    public class MonitorPanelTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? DefaultTemplate { get; set; }

        public DataTemplate? TimeTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is Core.MonitorPanel panel)
            {
                if (panel.Type == Core.MonitorType.Time && TimeTemplate != null)
                {
                    return TimeTemplate;
                }

                return DefaultTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
