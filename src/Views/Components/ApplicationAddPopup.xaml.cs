using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System;
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
        }

        public void LoadProcessesIfNeeded()
        {
            if (ProcessesListView.ItemsSource == null)
            {
                LoadProcesses();
            }
        }

        private void LoadProcesses()
        {
            var existingIds = new HashSet<string>(
                ExistingApplications
                    .Where(a => a.ID != null)
                    .Select(a => a.ID!)
            );


            var processes = Process.GetProcesses()
                .Where(p => p.MainWindowHandle != IntPtr.Zero)
                // 1. 先に ProcessName で重複を除外（MainModule へのアクセス回数を激減させる）
                .GroupBy(p => p.ProcessName)
                .Select(g => g.First())
                // 2. FileName の取得と並び替えを同時に行う（余計な匿名型を作らない）
                .OrderBy(p =>
                {
                    try { return p.MainModule?.ModuleName ?? p.ProcessName; }
                    catch { return p.ProcessName; }
                }, StringComparer.OrdinalIgnoreCase)
                // 3. 直接目的の型にマッピング
                .Select(p => new HardwareConfig
                {
                    ID = p.ProcessName,
                    Name = p.ProcessName,
                    ActualName = p.ProcessName,
                    Enabled = existingIds.Contains(p.ProcessName)
                })
                .ToArray();

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
