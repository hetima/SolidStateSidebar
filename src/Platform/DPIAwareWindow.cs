using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Media;
using Newtonsoft.Json;

namespace SSS.Windows
{
    public partial class DPIAwareWindow : Window
    {
        private static class WM_MESSAGES
        {
            public const int WM_DPICHANGED = 0x02E0;
            public const int WM_GETMINMAXINFO = 0x0024;
            public const int WM_SIZE = 0x0005;
            public const int WM_WINDOWPOSCHANGING = 0x0046;
            public const int WM_WINDOWPOSCHANGED = 0x0047;
        }

        public override void BeginInit()
        {
            Utilities.Culture.SetCurrent(false);

            base.BeginInit();
        }

        public override void EndInit()
        {
            base.EndInit();

            _originalWidth = base.Width;
            _originalHeight = base.Height;

            if (AutoDPI && OS.SupportDPI)
            {
                Loaded += DPIAwareWindow_Loaded;
            }
        }

        public void HandleDPI()
        {
            //IntPtr _hwnd = new WindowInteropHelper(this).Handle;

            //IntPtr _hmonitor = NativeMethods.MonitorFromWindow(_hwnd, 0);

            //Monitor _monitorInfo = Monitor.GetMonitor(_hmonitor);

            double _uiScale = Core.Settings.Instance.UIScale;

            UpdateScale(_uiScale, _uiScale, true);
        }

        public void UpdateScale(double scaleX, double scaleY, bool resize)
        {
            if (VisualChildrenCount > 0)
            {
                GetVisualChild(0).SetValue(LayoutTransformProperty, new ScaleTransform(scaleX, scaleY));
            }

            if (resize)
            {
                SizeToContent _autosize = SizeToContent;
                SizeToContent = SizeToContent.Manual;

                base.Width = _originalWidth * scaleX;
                base.Height = _originalHeight * scaleY;

                SizeToContent = _autosize;
            }
        }

        private void DPIAwareWindow_Loaded(object sender, RoutedEventArgs e)
        {
            HandleDPI();

            Core.Settings.Instance.PropertyChanged += UIScale_PropertyChanged;

            //HwndSource.AddHook(WindowHook);
        }

        private void UIScale_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "UIScale")
            {
                HandleDPI();
            }
        }

        //private IntPtr WindowHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        //{
        //    if (msg == WM_MESSAGES.WM_DPICHANGED)
        //    {
        //        HandleDPI();

        //        handled = true;
        //    }

        //    return IntPtr.Zero;
        //}

        public HwndSource HwndSource
        {
            get
            {
                return (HwndSource)PresentationSource.FromVisual(this);
            }
        }

        public static readonly DependencyProperty AutoDPIProperty = DependencyProperty.Register("AutoDPI", typeof(bool), typeof(DPIAwareWindow), new UIPropertyMetadata(true));

        public bool AutoDPI
        {
            get
            {
                return (bool)GetValue(AutoDPIProperty);
            }
            set
            {
                SetValue(AutoDPIProperty, value);
            }
        }

        public new double Width
        {
            get
            {
                return base.Width;
            }
            set
            {
                _originalWidth = base.Width = value;
            }
        }

        public new double Height
        {
            get
            {
                return base.Height;
            }
            set
            {
                _originalHeight = base.Height = value;
            }
        }

        private double _originalWidth;

        private double _originalHeight;
    }
}
