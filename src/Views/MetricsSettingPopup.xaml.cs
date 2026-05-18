using System.Windows;
using System.Windows.Controls;
using SSS.Core;

namespace SSS
{
    public partial class MetricsSettingPopup : UserControl
    {
        public MetricsSettingPopup()
        {
            InitializeComponent();
        }

        public static readonly RoutedEvent CloseRequestedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(CloseRequested),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(MetricsSettingPopup));

        public event RoutedEventHandler CloseRequested
        {
            add { AddHandler(CloseRequestedEvent, value); }
            remove { RemoveHandler(CloseRequestedEvent, value); }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CloseRequestedEvent));
        }

        private void MetricResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: MetricConfig mc })
            {
                mc.Label = null;
            }
        }
    }
}
