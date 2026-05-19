using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WPFDevelopers.Utilities;

namespace WPFDevelopers.Controls
{
    public struct HSB
    {
        public double H { get; set; }
        public double S { get; set; }
        public double B { get; set; }
    }
    
    public struct HSL
    {
        public double A;
        public double H;
        public double L;
        public double S;
    }

    [TemplatePart(Name = HueSliderColorTemplateName, Type = typeof(Slider))]
    [TemplatePart(Name = CanvasTemplateName, Type = typeof(Canvas))]
    [TemplatePart(Name = ThumbTemplateName, Type = typeof(Thumb))]
    public class ColorPicker : Control
    {
        private const string HueSliderColorTemplateName = "PART_HueSlider";
        private const string CanvasTemplateName = "PART_Canvas";
        private const string ThumbTemplateName = "PART_Thumb";
        private const string HexTextBoxTemplateName = "PART_HexTextBox";

        private static readonly DependencyPropertyKey HueColorPropertyKey =
            DependencyProperty.RegisterReadOnly("HueColor", typeof(Color), typeof(ColorPicker),
                new PropertyMetadata(Colors.Red, OnHueColorChanged));

        public static readonly DependencyProperty HueColorProperty = HueColorPropertyKey.DependencyProperty;

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Color), typeof(ColorPicker),
                new FrameworkPropertyMetadata(Colors.Red, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedColorChanged));

        private static readonly DependencyPropertyKey HSBPropertyKey =
            DependencyProperty.RegisterReadOnly("HSB", typeof(HSB), typeof(ColorPicker),
                new PropertyMetadata(new HSB()));

        public static readonly DependencyProperty HSBHProperty = HSBPropertyKey.DependencyProperty;

        private Canvas? _canvas;
        private TextBox? _hexTextBox;
        private Slider? _hueSliderColor;
        private bool _isInnerUpdateSelectedColor;
        private bool _isUpdatingFromSelectedColor;
        private Thumb? _thumb;

        static ColorPicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorPicker),
                new FrameworkPropertyMetadata(typeof(ColorPicker)));
        }

        public Color HueColor => (Color) GetValue(HueColorProperty);

        private static void OnHueColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColorPicker picker || picker._canvas == null) return;
            var hueColor = (Color)e.NewValue;
            var drawingBrush = new DrawingBrush
            {
                Drawing = new DrawingGroup
                {
                    Children = new DrawingCollection
                    {
                        new GeometryDrawing
                        {
                            Brush = new LinearGradientBrush
                            {
                                StartPoint = new Point(0, 0),
                                EndPoint = new Point(1, 0),
                                GradientStops = new GradientStopCollection
                                {
                                    new GradientStop(Colors.White, 0),
                                    new GradientStop(hueColor, 1)
                                }
                            },
                            Geometry = new RectangleGeometry(new Rect(0, 0, 5, 5))
                        },
                        new GeometryDrawing
                        {
                            Brush = new LinearGradientBrush
                            {
                                StartPoint = new Point(0, 0),
                                EndPoint = new Point(0, 1),
                                GradientStops = new GradientStopCollection
                                {
                                    new GradientStop(Colors.Transparent, 0),
                                    new GradientStop(Colors.Black, 1)
                                }
                            },
                            Geometry = new RectangleGeometry(new Rect(0, 0, 5, 5))
                        }
                    }
                }
            };
            picker._canvas.Background = drawingBrush;
        }

        public Color SelectedColor
        {
            get => (Color) GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public HSB HSB => (HSB) GetValue(HSBHProperty);

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as ColorPicker;
            if (ctrl == null)
            {
                return;
            }
            if (ctrl._isInnerUpdateSelectedColor)
            {
                ctrl._isInnerUpdateSelectedColor = false;
                return;
            }
            if (ctrl != null && ctrl._hueSliderColor == null) return;
            var color = (Color)e.NewValue;
            double h = 0, s = 0, b = 0;
            ColorUtil.HsbFromColor(color, ref h, ref s, ref b);
            var hsb = new HSB { H = h, S = s, B = b };
            ctrl!.SetValue(HueColorPropertyKey, ColorUtil.ColorFromHsb(hsb.H, 1, 1));
            ctrl.SetValue(HSBPropertyKey, hsb);
            ctrl.UpdateThumbPosition();
            ctrl._isUpdatingFromSelectedColor = true;
            ctrl._hueSliderColor!.Value = 1 - h;
            ctrl._isUpdatingFromSelectedColor = false;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_hueSliderColor != null)
                _hueSliderColor.ValueChanged -= HueSliderColor_OnValueChanged;
            if (_hexTextBox != null)
                _hexTextBox.KeyDown -= HexTextBox_OnKeyDown;
            if (_canvas != null)
                _canvas.MouseUp -= Canvas_MouseUp;
            if (_thumb != null)
                _thumb.DragDelta -= Thumb_DragDelta;

            _canvas = GetTemplateChild(CanvasTemplateName) as Canvas;
            if (_canvas != null)
                _canvas.MouseUp += Canvas_MouseUp;

            _thumb = GetTemplateChild(ThumbTemplateName) as Thumb;
            if (_thumb != null)
                _thumb.DragDelta += Thumb_DragDelta;
            _hueSliderColor = GetTemplateChild(HueSliderColorTemplateName) as Slider;
            if (_hueSliderColor != null)
                _hueSliderColor.ValueChanged += HueSliderColor_OnValueChanged;

            _hexTextBox = GetTemplateChild(HexTextBoxTemplateName) as TextBox;
            if (_hexTextBox != null)
                _hexTextBox.KeyDown += HexTextBox_OnKeyDown;

            LoadedColor();
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_canvas == null) return;
            var canvasPosition = e.GetPosition(_canvas);
            GetHSB(canvasPosition);
        }

        private void GetHSB(Point point, bool isMove = true)
        {
            if (_canvas == null || _thumb == null || _hueSliderColor == null) return;
            var newLeft = point.X - _thumb.ActualWidth / 2;
            var newTop = point.Y - _thumb.ActualHeight / 2;
            var thumbW = _thumb.ActualWidth / 2;
            var thumbH = _thumb.ActualHeight / 2;
            var canvasRight = _canvas.ActualWidth - thumbW;
            var canvasBottom = _canvas.ActualHeight - thumbH;
            if (newLeft < -thumbW)
                newLeft = -thumbW;
            else if (newLeft > canvasRight)
                newLeft = canvasRight;
            if (newTop < -thumbH)
                newTop = -thumbH;
            else if (newTop > canvasBottom)
                newTop = canvasBottom;

            if (isMove)
            {
                Canvas.SetLeft(_thumb, newLeft);
                Canvas.SetTop(_thumb, newTop);
            }

            var hsb = new HSB
            {
                H = HSB.H,
                S = (newLeft + thumbW) / _canvas.ActualWidth,
                B = 1 - (newTop + thumbH) / _canvas.ActualHeight
            };
            SetValue(HSBPropertyKey, hsb);
            var currentColor = ColorUtil.ColorFromAhsb(1, HSB.H, HSB.S, HSB.B);
            if (SelectedColor != currentColor)
            {
                _isInnerUpdateSelectedColor = true;
                SelectedColor = currentColor;
            }
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_canvas == null) return;
            var point = Mouse.GetPosition(_canvas);
            GetHSB(point);
        }

        private void LoadedColor()
        {
            if (_canvas == null || _thumb == null || _hueSliderColor == null) return;
            var width = (int)_canvas.ActualWidth;
            var height = (int)_canvas.ActualHeight;
            var point = new Point(width - _thumb.ActualWidth / 2, -_thumb.ActualHeight / 2);
            Canvas.SetLeft(_thumb, point.X);
            Canvas.SetTop(_thumb, point.Y);
            var hsb = new HSB { H = _hueSliderColor.Value, S = HSB.S, B = HSB.B };
            SetValue(HSBPropertyKey, hsb);
        }

        private void HueSliderColor_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingFromSelectedColor)
                return;
            if (_thumb == null)
                return;
            if (DoubleUtil.AreClose(HSB.H, 1 - e.NewValue))
                return;
            var hsb = new HSB { H = 1 - e.NewValue, S = HSB.S, B = HSB.B };
            SetValue(HSBPropertyKey, hsb);
            SetValue(HueColorPropertyKey, ColorUtil.ColorFromHsb(HSB.H, 1, 1));

            var newLeft = Canvas.GetLeft(_thumb);
            var newTop = Canvas.GetTop(_thumb);
            var point = new Point(newLeft, newTop);
            GetHSB(point, false);
        }

        private void UpdateThumbPosition()
        {
            if (_canvas == null || _thumb == null) return;
            var color = SelectedColor;
            double h = 0, s = 0, b = 0;
            ColorUtil.HsbFromColor(color, ref h, ref s, ref b);
            Canvas.SetLeft(_thumb, s * _canvas.ActualWidth - _thumb.ActualWidth / 2);
            Canvas.SetTop(_thumb, (1 - b) * _canvas.ActualHeight - _thumb.ActualHeight / 2);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            UpdateThumbPosition();
        }

        private void HexTextBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is TextBox textBox)
                {
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateSource();
                    textBox.SelectAll();
                }
            }
        }
    }
}
