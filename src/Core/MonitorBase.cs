using System;
using System.ComponentModel;

namespace SSS.Core
{
    public interface iMonitor : INotifyPropertyChanged, IDisposable
    {
        string? ID { get; }

        string? Name { get; }

        bool ShowName { get; }

        iMetric[]? Metrics { get; }

        void Update();
    }

    public class BaseMonitor : iMonitor
    {
        public BaseMonitor(string id, string name, bool showName)
        {
            ID = id;
            Name = name;
            ShowName = showName;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && Metrics != null)
                {
                    foreach (iMetric _metric in Metrics)
                    {
                        _metric.Dispose();
                    }

                    _metrics = null;
                }

                _disposed = true;
            }
        }

        ~BaseMonitor()
        {
            Dispose(false);
        }

        public virtual void Update()
        {
            if (Metrics != null)
            {
                foreach (iMetric _metric in Metrics)
                {
                    _metric.Update();
                }
            }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private string? _id;

        public string? ID
        {
            get
            {
                return _id;
            }
            protected set
            {
                _id = value;

                NotifyPropertyChanged(nameof(ID));
            }
        }

        private string? _name;

        public string? Name
        {
            get
            {
                return _name;
            }
            protected set
            {
                _name = value;

                NotifyPropertyChanged(nameof(Name));
            }
        }

        private bool _showName;

        public bool ShowName
        {
            get
            {
                return _showName;
            }
            protected set
            {
                _showName = value;

                NotifyPropertyChanged(nameof(ShowName));
            }
        }

        private iMetric[]? _metrics;

        public iMetric[]? Metrics
        {
            get
            {
                return _metrics;
            }
            protected set
            {
                _metrics = value;

                NotifyPropertyChanged(nameof(Metrics));
            }
        }

        protected bool _disposed = false;
    }
}
