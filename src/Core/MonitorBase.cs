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

    public class BaseMonitor : ObservableObject, iMonitor
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

        private string? _id;

        public string? ID
        {
            get => _id;
            protected set => SetProperty(ref _id, value);
        }

        private string? _name;

        public string? Name
        {
            get => _name;
            protected set => SetProperty(ref _name, value);
        }

        private bool _showName;

        public bool ShowName
        {
            get => _showName;
            protected set => SetProperty(ref _showName, value);
        }

        private iMetric[]? _metrics;

        public iMetric[]? Metrics
        {
            get => _metrics;
            protected set => SetProperty(ref _metrics, value);
        }

        protected bool _disposed = false;
    }
}
