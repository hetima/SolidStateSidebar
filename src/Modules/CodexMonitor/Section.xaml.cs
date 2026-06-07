using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SVGImage.SVG;
using SSS.Core;
using SSS.Utilities;
using CoreSettings = SSS.Core.Settings;

namespace SSS.Module.CodexMonitor
{
    public partial class Section : UserControl
    {
        public Section()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is not MonitorPanel panel) return;

            // 自動更新開始（初回取得含む）
            foreach (var monitor in panel.Monitors.OfType<CodexMonitor>())
                monitor.StartAutoRefresh(panel.AutoRefresh, panel.ShortResetDisplay, panel.LongResetDisplay);

            var style = panel.SectionHeaderStyle;

            // None のときヘッダー行ごと非表示
            HeaderPanel.Visibility = style == SectionHeaderStyle.None
                ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

            // NoIcon/None のときアイコン非表示
            IconImage.Visibility = (style == SectionHeaderStyle.NoIcon || style == SectionHeaderStyle.None)
                ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

            if (style != SectionHeaderStyle.None && style != SectionHeaderStyle.NoIcon)
            {
                // Small のときはアイコンサイズを小さくする
                if (style == SectionHeaderStyle.Small)
                {
                    double sz = CoreSettings.Instance.SmallIconSize;
                    IconImage.Width = sz;
                    IconImage.Height = sz;
                }

                string svgPath = EmbeddedSvg.Extract("codex.svg", "codex.svg");
                LoadSvgIcon(svgPath);
            }
        }

        private void LoadSvgIcon(string path)
        {
            if (!File.Exists(path)) return;
            var render = new SVGRender();
            Color clr = CoreSettings.Instance.FontColorColor;
            render.OverrideColor = clr;
            render.OverrideFillColor = clr;
            DrawingGroup? drawing = render.LoadDrawing(path);
            if (drawing != null)
                IconImage.Source = new DrawingImage(drawing);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MonitorPanel panel) return;
            foreach (var monitor in panel.Monitors.OfType<CodexMonitor>())
                monitor.ManualRefresh(panel.ShortResetDisplay, panel.LongResetDisplay);
        }
    }
}
