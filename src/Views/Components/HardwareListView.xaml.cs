using System.Windows.Controls;

namespace SSS.Views.Components
{
    public partial class HardwareListView : UserControl
    {
        public HardwareListView()
        {
            InitializeComponent();
        }

        private void HardwareResetButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is Button { DataContext: Core.HardwareConfig hw })
            {
                hw.Name = hw.ActualName;
            }
        }
    }
}
