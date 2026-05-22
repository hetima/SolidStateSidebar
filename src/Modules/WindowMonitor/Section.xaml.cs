using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SSS.Core;

namespace SSS.Module.WindowMonitor
{
    public partial class Section : UserControl
    {
        public Section()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }


        /// <summary>
        /// 正しくbindingさせる方法が分からなかったのでフォントを直接設定する。現状、設定変更==再生成なので問題はない。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is MonitorPanel panel)
            {
                if (panel.FontSize > 0)
                    FontSize = panel.FontSize;

                if (!string.IsNullOrEmpty(panel.FontName))
                    FontFamily = new FontFamily(panel.FontName);
            }
        }
    }
}