using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace SidebarDiagnostics
{
    /// <summary>
    /// Interaction logic for SettingsCustomizeView.xaml
    /// </summary>
    public partial class SettingsCustomizeView : UserControl
    {
        public SettingsCustomizeView()
        {
            InitializeComponent();
        }

        private void NumberBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (new Regex("[^0-9.-]+").IsMatch(e.Text))
            {
                e.Handled = true;
            }
        }
    }
}