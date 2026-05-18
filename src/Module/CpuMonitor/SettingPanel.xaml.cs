using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace SSS.Module.CpuMonitor
{
    public partial class SettingPanel : UserControl
    {
        public SettingPanel()
        {
            InitializeComponent();
        }

        private void MetricsBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                MetricsPopup.PlacementTarget = window;
                MetricsPopup.Placement = PlacementMode.Center;
            }
            MetricsPopup.IsOpen = true;
        }

        private void MetricsPopup_Loaded(object sender, RoutedEventArgs e)
        {
            MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        }

        private void MetricsPopup_CloseRequested(object sender, RoutedEventArgs e)
        {
            MetricsPopup.IsOpen = false;
        }
    }
}
