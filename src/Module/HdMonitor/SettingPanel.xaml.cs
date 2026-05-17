using System.Windows.Controls;

namespace SSS.Module.HdMonitor
{
    public partial class SettingPanel : UserControl
    {
        public SettingPanel()
        {
            InitializeComponent();
        }

        private void HardwareResetButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button { DataContext: Core.HardwareConfig hw })
            {
                hw.Name = hw.ActualName;
            }
        }
    }
}
