using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace SSS.Module.TimeMonitor
{
    public partial class SettingPanel : UserControl
    {
        public SettingPanel()
        {
            InitializeComponent();
        }

        private void NumberBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Only allow numeric input
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]*$");
        }
    }
}
