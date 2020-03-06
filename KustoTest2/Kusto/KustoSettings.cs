using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KustoTest2.Kusto
{
    public enum AuthMode
    {
        SPN,
        User
    }

    public class KustoSettings
    {
        private string _clusterUrl;
        public string ClusterName { get; set; }
        public string RegionName { get; set; }
        public string DbName { get; set; }
        public AuthMode AuthMode { get; set; } = AuthMode.SPN;
        public string ClusterUrl
        {
            get
            {
                return _clusterUrl ?? (string.IsNullOrEmpty(RegionName)
                    ? $"https://{ClusterName}.kusto.windows.net"
                    : $"https://{ClusterName}.{RegionName}.kusto.windows.net");
            }
            set
            {
                _clusterUrl = value;
            }
        }
        public SyncSettings[] Tables { get; set; }
        
    }

    public class SyncSettings
    {
        public string Query { get; set; }
        public string Model { get; set; }
        public string DocDb { get; set; }
        public string Collection { get; set; }
        public bool ClearTarget { get; set; } = false;
        public bool SplitByDc { get; set; }
    }
}
