using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace SidebarDiagnostics
{
    /// <summary>
    /// Interaction logic for SettingsAdvancedView.xaml
    /// </summary>
    public partial class SettingsAdvancedView : UserControl
    {
        public SettingsAdvancedView()
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

        private void OffsetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.NewValue != 0d)
            {
                ShowTrayIconCheckbox.IsChecked = true;
            }
        }

        private void ClickThroughCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            ShowTrayIconCheckbox.IsChecked = true;
        }

        private void ShowTrayIconCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            XOffsetSlider.Value = 0d;
            YOffsetSlider.Value = 0d;
            ClickThroughCheckbox.IsChecked = false;
        }
    }
}