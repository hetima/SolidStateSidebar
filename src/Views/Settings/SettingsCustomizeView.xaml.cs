using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace SSS
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
            if (NonNumericRegex().IsMatch(e.Text))
            {
                e.Handled = true;
            }
        }

        [GeneratedRegex("[^0-9.-]+")]
        private static partial Regex NonNumericRegex();
    }
}