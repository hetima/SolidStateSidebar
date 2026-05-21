using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using SSS.Core;
using SSS.Views.Components;

namespace SSS.Module.WindowMonitor
{
    public partial class SettingPanel : UserControl
    {
        public SettingPanel()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is Data data)
            {
                ApplicationAddPopupCtrl.ExistingApplications = data.ApplicationOC ?? [];
            }

            ApplicationAddPopupCtrl.LoadProcessesIfNeeded();

            var window = Window.GetWindow(this);
            if (window != null)
            {
                ApplicationPopup.PlacementTarget = window;
                ApplicationPopup.Placement = PlacementMode.Center;
            }
            ApplicationPopup.IsOpen = true;
        }

        private void NumberBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]*$");
        }

        private void ApplicationPopup_CloseRequested(object sender, RoutedEventArgs e)
        {
            ApplicationPopup.IsOpen = false;

            if (DataContext is Data data && data.ApplicationOC != null)
            {
                var existingIds = new HashSet<string>(
                    data.ApplicationOC.Where(a => a.ID != null).Select(a => a.ID!)
                );

                var selected = ApplicationAddPopupCtrl.GetSelectedApplications();

                foreach (var app in selected)
                {
                    if (app.ID != null && !existingIds.Contains(app.ID))
                    {
                        data.ApplicationOC.Add(app);
                    }
                }
            }
        }
    }
}
