using System;
using System.ComponentModel;
using System.Windows.Threading;

namespace SSS.Core
{
    public interface iMetric : INotifyPropertyChanged, IDisposable
    {
        MetricKey Key { get; }

        string? FullName { get; }

        string? CustomLabel { get; set; }

        string Label { get; }

        double Value { get; }

        string Append { get; }

        double nValue { get; }

        string nAppend { get; }

        string? Text { get; }

        bool IsAlert { get; }

        bool IsNumeric { get; }

        void Update();

        void Update(double value);
    }

    public class BaseMetric : ObservableObject, iMetric
    {
        public BaseMetric(MetricKey key, DataType dataType, string? label = null, bool round = false, double alertValue = 0, iConverter? converter = null)
        {
            _converter = converter;
            _round = round;
            _alertValue = alertValue;

            Key = key;

            if (label == null)
            {
                FullName = key.GetFullName();
                Label = key.GetLabel();
            }
            else
            {
                FullName = Label = label;
            }

            nAppend = Append = converter == null ? dataType.GetAppend() : converter.TargetType.GetAppend();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _alertColorTimer?.Stop();
                    _alertColorTimer = null;

                    _converter = null;
                }

                _disposed = true;
            }
        }

        public virtual void Update() { }

        public void Update(double value)
        {
            double _val = value;

            if (_converter == null)
            {
                nValue = _val;
            }
            else if (_converter.IsDynamic)
            {
                double _nVal;
                DataType _dataType;

                _converter.Convert(ref _val, out _nVal, out _dataType);

                nValue = _nVal;
                Append = _dataType.GetAppend();
            }
            else
            {
                _converter.Convert(ref _val);

                nValue = _val;
            }

            Value = _val;

            if (_alertValue > 0 && _alertValue <= nValue)
            {
                if (!IsAlert)
                {
                    IsAlert = true;
                }
            }
            else if (IsAlert)
            {
                IsAlert = false;
            }

            Text = string.Format(
                "{0:#,##0.##}{1}",
                _val.Round(_round ?? false),
                Append
                );
        }

        private MetricKey _key;

        public MetricKey Key
        {
            get => _key;
            protected set => SetProperty(ref _key, value);
        }

        private string? _fullName;

        public string? FullName
        {
            get => _fullName;
            protected set => SetProperty(ref _fullName, value);
        }

        private string? _label;

        private string? _customLabel;

        public string? CustomLabel
        {
            get => _customLabel;
            set
            {
                if (_customLabel == value) return;

                _customLabel = value;

                NotifyPropertyChanged(nameof(CustomLabel));
                NotifyPropertyChanged(nameof(Label));
            }
        }

        public string Label
        {
            get => _customLabel ?? _label ?? string.Empty;
            protected set => SetProperty(ref _label, value);
        }

        private double _value;

        public double Value
        {
            get => _value;
            protected set => SetProperty(ref _value, value);
        }

        private string? _append;

        public string Append
        {
            get => _append ?? string.Empty;
            protected set => SetProperty(ref _append, value);
        }

        private double _nValue;

        public double nValue
        {
            get => _nValue;
            set => SetProperty(ref _nValue, value);
        }

        private string? _nAppend;

        public string nAppend
        {
            get => _nAppend ?? string.Empty;
            set => SetProperty(ref _nAppend, value);
        }

        private string? _text;

        public string? Text
        {
            get => _text;
            protected set => SetProperty(ref _text, value);
        }

        private bool _isAlert;

        public bool IsAlert
        {
            get => _isAlert;
            protected set
            {
                _isAlert = value;

                NotifyPropertyChanged(nameof(IsAlert));

                if (value)
                {
                    _alertColorFlag = false;

                    if (Core.Settings.Instance.AlertBlink)
                    {
                        _alertColorTimer = new DispatcherTimer(DispatcherPriority.Normal, App.Current.Dispatcher);
                        _alertColorTimer.Interval = TimeSpan.FromSeconds(0.5d);
                        _alertColorTimer.Tick += new EventHandler(AlertColorTimer_Tick);
                        _alertColorTimer.Start();
                    }
                }
                else
                {
                    _alertColorTimer?.Stop();
                    _alertColorTimer = null;
                }
            }
        }

        public virtual bool IsNumeric
        {
            get { return true; }
        }

        public string AlertColor
        {
            get
            {
                return _alertColorFlag ? Settings.Instance.FontColor : Settings.Instance.AlertFontColor;
            }
        }

        private DispatcherTimer? _alertColorTimer;

        private void AlertColorTimer_Tick(object? sender, EventArgs e)
        {
            _alertColorFlag = !_alertColorFlag;

            NotifyPropertyChanged(nameof(AlertColor));
        }

        private bool _alertColorFlag = false;

        protected iConverter? _converter;

        protected bool? _round;

        protected double? _alertValue;

        protected bool _disposed = false;
    }
}
