using SSS.Core;
using SSS.Windows;

namespace SSS.Models
{
    public class DockItem
    {
        public DockEdge Value { get; set; }

        public string? Text { get; set; }
    }

    public class ScreenItem
    {
        public int Index { get; set; }

        public string? Text { get; set; }
    }

    public class TextAlignItem
    {
        public TextAlign Value { get; set; }

        public string? Text { get; set; }
    }
}
