using System.Windows.Controls;
using SSS.Core;

namespace SSS.Views.Components
{
    public partial class ApplicationListView : UserControl
    {
        public ApplicationListView()
        {
            InitializeComponent();
        }

        private void DeleteButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button { DataContext: HardwareConfig item })
            {
                if (DataContext is SSS.Module.WindowMonitor.Data data && data.ApplicationOC != null)
                {
                    data.ApplicationOC.Remove(item);
                }
            }
        }
    }
}
