using System;
using System.Diagnostics;
using LibreHardwareMonitor.Hardware;

namespace SSS.Core
{
    public class OHMMetric : BaseMetric
    {
        public OHMMetric(ISensor sensor, MetricKey key, DataType dataType, string? label = null, bool round = false, double alertValue = 0, iConverter? converter = null) : base(key, dataType, label, round, alertValue, converter)
        {
            _sensor = sensor;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _sensor = null;
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }


        public override void Update()
        {
            if (_sensor?.Value.HasValue == true)
            {
                Update(_sensor.Value.Value);
            }
            else
            {
                Text = "No Value";
            }
        }

        private ISensor? _sensor;
    }

    public class GPUVRAMMLoadMetric : BaseMetric
    {
        public GPUVRAMMLoadMetric(ISensor memoryUsedSensor, ISensor memoryTotalSensor, MetricKey key, DataType dataType, string? label = null, bool round = false, double alertValue = 0, iConverter? converter = null) : base(key, dataType, label, round, alertValue, converter)
        {
            _memoryUsedSensor = memoryUsedSensor;
            _memoryTotalSensor = memoryTotalSensor;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _memoryUsedSensor = null;
                    _memoryTotalSensor = null;
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }


        public override void Update()
        {
            if (_memoryUsedSensor!.Value.HasValue && _memoryTotalSensor!.Value.HasValue)
            {
                float load = _memoryUsedSensor.Value.Value / _memoryTotalSensor.Value.Value * 100f;

                Update(load);
            }
            else
            {
                Text = "No Value";
            }
        }

        private ISensor? _memoryUsedSensor;

        private ISensor? _memoryTotalSensor;
    }

    public class IPMetric : BaseMetric
    {
        public IPMetric(string ipAddress, MetricKey key, DataType dataType, string? label = null, bool round = false, double alertValue = 0, iConverter? converter = null) : base(key, dataType, label, round, alertValue, converter)
        {
            Text = ipAddress;
        }


        public override bool IsNumeric
        {
            get { return false; }
        }
    }

    public class PCMetric : BaseMetric
    {
        public PCMetric(PerformanceCounter counter, MetricKey key, DataType dataType, string? label = null, bool round = false, double alertValue = 0, iConverter? converter = null) : base(key, dataType, label, round, alertValue, converter)
        {
            _counter = counter;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _counter?.Dispose();
                    _counter = null;
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }


        public override void Update()
        {
            Update(_counter!.NextValue());
        }

        private PerformanceCounter? _counter;
    }
}
