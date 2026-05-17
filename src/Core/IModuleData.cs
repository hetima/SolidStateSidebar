using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SSS.Core
{
    public interface IModuleData : INotifyPropertyChanged
    {
        string Name { get; }
        bool Enabled { get; set; }
        byte Order { get; set; }
        HardwareConfig[] Hardware { get; set; }
        MetricConfig[] Metrics { get; set; }
        ObservableCollection<HardwareConfig>? HardwareOC { get; set; }
        IModuleData Clone();
    }
}
