using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SSS.Core;

namespace SSS.Views.Components
{
    public partial class ApplicationAddPopup : UserControl
    {
        public ObservableCollection<HardwareConfig> ExistingApplications { get; set; }

        public ApplicationAddPopup()
        {
            ExistingApplications = [];
            InitializeComponent();
            Focusable = true;
            Loaded += ApplicationAddPopup_Loaded;
        }

        private void ApplicationAddPopup_Loaded(object sender, RoutedEventArgs e)
        {
            LoadProcesses();
        }

        private void LoadProcesses()
        {
            var existingIds = new HashSet<string>(
                ExistingApplications
                    .Where(a => a.ID != null)
                    .Select(a => a.ID!)
            );

            var processes = Process.GetProcesses()
                .Where(p => !string.IsNullOrWhiteSpace(p.MainWindowTitle))
                .Select(p =>
                {
                    string fileName = "";
                    try { fileName = System.IO.Path.GetFileName(p.MainModule?.FileName) ?? p.ProcessName; }
                    catch { fileName = p.ProcessName; }
                    return new { p.ProcessName, FileName = fileName };
                })
                .GroupBy(x => x.ProcessName)
                .Select(g => g.First())
                .OrderBy(x => x.FileName, System.StringComparer.OrdinalIgnoreCase)
                .Select(x => new HardwareConfig
                {
                    ID = x.ProcessName,
                    Name = x.FileName,
                    ActualName = x.FileName,
                    Enabled = existingIds.Contains(x.ProcessName)
                })
                .ToArray(); // evaluate before Dispose

            ProcessesListView.ItemsSource = new ObservableCollection<HardwareConfig>(processes);
        }

        public static readonly RoutedEvent CloseRequestedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(CloseRequested),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ApplicationAddPopup));

        public event RoutedEventHandler CloseRequested
        {
            add { AddHandler(CloseRequestedEvent, value); }
            remove { RemoveHandler(CloseRequestedEvent, value); }
        }

        public ObservableCollection<HardwareConfig> GetSelectedApplications()
        {
            var selected = new ObservableCollection<HardwareConfig>();

            if (ProcessesListView.ItemsSource is ObservableCollection<HardwareConfig> items)
            {
                foreach (var item in items)
                {
                    if (item.Enabled)
                    {
                        selected.Add(new HardwareConfig
                        {
                            ID = item.ID,
                            Name = item.ActualName,
                            ActualName = item.ActualName,
                            Enabled = true
                        });
                    }
                }
            }

            return selected;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CloseRequestedEvent));
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                RaiseEvent(new RoutedEventArgs(CloseRequestedEvent));
                e.Handled = true;
            }
        }
    }
}
