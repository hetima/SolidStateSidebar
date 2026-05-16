using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using SidebarDiagnostics.Models;
using SidebarDiagnostics.Windows;
using SidebarDiagnostics.Style;

namespace SidebarDiagnostics
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : FlatWindow
    {
        public Settings(Sidebar sidebar)
        {
            InitializeComponent();

            DataContext = Model = new SettingsModel(sidebar);

            Owner = sidebar;
            ShowDialog();
        }

        private async Task Save(bool finalize)
        {
            Model.Save();

            await App.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(async () =>
            {
                Sidebar _sidebar = App.Current.Sidebar;

                if (_sidebar == null)
                {
                    return;
                }

                await _sidebar.Reset(finalize);
            }));
        }
        
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await Save(true);

            Close();
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            await Save(false);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Model.IsChanged)
            {
                Sidebar _sidebar = App.Current.Sidebar;

                if (_sidebar != null)
                {
                    DataContext = Model = new SettingsModel(_sidebar);
                    return;
                }
            }

            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Hotkey.Disable();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            DataContext = null;
            Model = null;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Hotkey.Enable();
        }

        public SettingsModel Model { get; private set; }
    }
}
