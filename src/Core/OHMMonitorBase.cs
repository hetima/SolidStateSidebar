using System;
using System.Linq;
using System.Text.RegularExpressions;
using LibreHardwareMonitor.Hardware;

namespace SSS.Core
{
    public abstract partial class OHMMonitorBase : BaseMonitor
    {
        protected OHMMonitorBase(string id, string name, IHardware hardware, bool showHardwareNames) : base(id, name, showHardwareNames)
        {
            _hardware = hardware;

            UpdateHardware();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _hardware = null;
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }

        ~OHMMonitorBase()
        {
            Dispose(false);
        }

        public override void Update()
        {
            UpdateHardware();

            base.Update();
        }

        /// <summary>
        /// Common LINQ query for creating monitor instances from hardware and config arrays.
        /// </summary>
        protected static T[] CreateInstances<T>(
            HardwareConfig[] hardwareConfig,
            IHardware[] hardware,
            Func<HardwareConfig, IHardware, T> factory) where T : iMonitor
        {
            return (
                from hw in hardware
                join c in hardwareConfig on hw.Identifier.ToString() equals c.ID into merged
                from n in merged.DefaultIfEmpty(new HardwareConfig() { ID = hw.Identifier.ToString(), Name = hw.Name, ActualName = hw.Name }).Select(n => { if (n.ActualName != hw.Name) { n.Name = n.ActualName = hw.Name; } return n; })
                where n.Enabled
                orderby n.Order descending, n.Name ascending
                select factory(n, hw)
                ).ToArray();
        }

        private void UpdateHardware()
        {
            _hardware!.Update();
        }

        protected IHardware? Hardware => _hardware;

        private IHardware? _hardware;

        protected static ISensor? FindSensor(ISensor[] sensors, Func<ISensor, bool> predicate)
        {
            return sensors.FirstOrDefault(predicate);
        }

        protected static ISensor? FindSensorWithFallback(ISensor[]? fallback, ISensor[] primary, Func<ISensor, bool> fallbackPredicate, Func<ISensor, bool> primaryPredicate)
        {
            if (fallback != null)
            {
                ISensor? sensor = fallback.FirstOrDefault(fallbackPredicate);

                if (sensor != null)
                {
                    return sensor;
                }
            }

            return primary.FirstOrDefault(primaryPredicate);
        }

        protected static ISensor[] FindSensors(ISensor[] sensors, Func<ISensor, bool> predicate)
        {
            return sensors.Where(predicate).ToArray();
        }
    }
}
