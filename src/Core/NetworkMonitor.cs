using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SSS.Core
{
    public partial class NetworkMonitor : BaseMonitor
    {
        private const string CATEGORYNAME = "Network Interface";

        private const string BYTESRECEIVEDPERSECOND = "Bytes Received/sec";
        private const string BYTESSENTPERSECOND = "Bytes Sent/sec";

        public NetworkMonitor(string id, string name, string? extIP, MetricConfig[] metrics, bool showName = true, bool roundAll = false, bool useBytes = false, double bandwidthInAlert = 0, double bandwidthOutAlert = 0) : base(id, name, showName)
        {
            iConverter _converter;

            if (useBytes)
            {
                _converter = BytesPerSecondConverter.Instance;
            }
            else
            {
                _converter = BitsPerSecondConverter.Instance;
            }

            List<iMetric> _metrics = [];

            if (metrics.IsEnabled(MetricKey.NetworkIP))
            {
                string? _ipAddress = GetAdapterIPAddress(name);

                if (!string.IsNullOrEmpty(_ipAddress))
                {
                    _metrics.Add(new IPMetric(_ipAddress, MetricKey.NetworkIP, DataType.IP));
                }
            }

            if (!string.IsNullOrEmpty(extIP))
            {
                _metrics.Add(new IPMetric(extIP, MetricKey.NetworkExtIP, DataType.IP));
            }

            if (metrics.IsEnabled(MetricKey.NetworkIn))
            {
                _metrics.Add(new PCMetric(new PerformanceCounter(CATEGORYNAME, BYTESRECEIVEDPERSECOND, id), MetricKey.NetworkIn, DataType.kbps, null, roundAll, bandwidthInAlert, _converter));
            }

            if (metrics.IsEnabled(MetricKey.NetworkOut))
            {
                _metrics.Add(new PCMetric(new PerformanceCounter(CATEGORYNAME, BYTESSENTPERSECOND, id), MetricKey.NetworkOut, DataType.kbps, null, roundAll, bandwidthOutAlert, _converter));
            }

            Metrics = _metrics.ToArray();
            metrics.ApplyCustomLabels(Metrics);
        }

        ~NetworkMonitor()
        {
            Dispose(false);
        }

        public static IEnumerable<HardwareConfig> GetHardware()
        {
            string[] _instances;

            try
            {
                _instances = new PerformanceCounterCategory(CATEGORYNAME).GetInstanceNames();
            }
            catch (InvalidOperationException)
            {
                _instances = [];
            }

            return _instances.Where(i => !IsatapRegex().IsMatch(i)).OrderBy(h => h).Select(h => new HardwareConfig() { ID = h, Name = h, ActualName = h });
        }

        public static iMonitor[] GetInstances(HardwareConfig[] hardwareConfig, MetricConfig[] metrics, bool showName, bool roundAll, bool useBytes, int bandwidthInAlert, int bandwidthOutAlert)
        {
            string? _extIP = null;

            if (metrics.IsEnabled(MetricKey.NetworkExtIP))
            {
                _extIP = GetExternalIPAddressAsync().GetAwaiter().GetResult();
            }

            return (
                from hw in GetHardware()
                join c in hardwareConfig on hw.ID equals c.ID into merged
                from n in merged.DefaultIfEmpty(hw).Select(n => { n.ActualName = hw.Name; return n; })
                where n.Enabled
                orderby n.Order descending, n.Name ascending
                select new NetworkMonitor(n.ID ?? hw.ID!, n.Name ?? n.ActualName!, _extIP, metrics, showName, roundAll, useBytes, bandwidthInAlert, bandwidthOutAlert)
                ).ToArray();
        }

        public override void Update()
        {
            if (!PerformanceCounterCategory.InstanceExists(ID, CATEGORYNAME))
            {
                return;
            }

            base.Update();
        }

        private static string? GetAdapterIPAddress(string name)
        {
            //Here we need to match the apdapter returned by the network interface to the
            //adapter represented by this instance of the class.

            string configuredName = SpecialCharRegex().Replace(name, "");

            foreach (NetworkInterface netif in NetworkInterface.GetAllNetworkInterfaces())
            {
                //Strange pattern matching as the Performance Monitor routines which provide the ID and Names
                //instantiating this class return different values for the devices than the NetworkInterface calls used here.
                //For example Performance Monitor routines return Intel[R] where as NetworkInterface returns Intel(R) causing the
                //strings not to match.  So to get around this, use Regex to strip off the special characters and just compare the string values.
                //Also, in some cases the values for Description match the Performance Monitor calls, and 
                //in others the Name is what matches.  It's a little weird, but this will pick up all 4 network adapters on 
                //my test machine correctly.

                string interfaceDesc = SpecialCharRegex().Replace(netif.Description, "");
                string interfaceName = SpecialCharRegex().Replace(netif.Name, "");

                if (interfaceDesc == configuredName || interfaceName == configuredName)
                {
                    IPInterfaceProperties properties = netif.GetIPProperties();

                    foreach (IPAddressInformation unicast in properties.UnicastAddresses)
                    {
                        if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return unicast.Address.ToString();
                        }
                    }
                }
            }

            return null;
        }

        [GeneratedRegex(@"^isatap.*$")]
        private static partial Regex IsatapRegex();

        [GeneratedRegex(@"[^\w\d\s]")]
        private static partial Regex SpecialCharRegex();

        private static readonly HttpClient Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private static async Task<string> GetExternalIPAddressAsync()
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, Constants.URLs.IPIFY);
                var res = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead)
                                    .ConfigureAwait(false);
                res.EnsureSuccessStatusCode();

                var ip = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                return ip.Trim();
            }
            catch (HttpRequestException)
            {
                return "";
            }
            catch (TaskCanceledException) // timeout or cancellation
            {
                return "";
            }
        }
    }
}
