using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using GongSolutions.Wpf.DragDrop;
using SSS.Core;

namespace SSS
{
    /// <summary>
    /// Interaction logic for SettingsMonitorsView.xaml
    /// </summary>
    public partial class SettingsMonitorsView : UserControl, IDropTarget
    {
        public SettingsMonitorsView()
        {
            InitializeComponent();
        }

        void IDropTarget.DragOver(IDropInfo? dropInfo)
        {
            DragDrop.DefaultDropHandler.DragOver(dropInfo);
        }

        void IDropTarget.Drop(IDropInfo? dropInfo)
        {
            DragDrop.DefaultDropHandler.Drop(dropInfo);

            // ドロップ後にドラッグした項目を選択状態にする
            MonitorConfig? target = dropInfo?.Data switch
            {
                MonitorConfig item => item,
                IEnumerable<MonitorConfig> items => items.FirstOrDefault(),
                _ => null
            };
            if (target != null)
            {
                MonitorListView.SelectedItem = target;
            }
        }

        private void HardwareResetButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button { DataContext: HardwareConfig hw })
            {
                hw.Name = hw.ActualName;
            }
        }
    }
}