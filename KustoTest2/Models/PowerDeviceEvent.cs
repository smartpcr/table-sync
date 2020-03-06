using Newtonsoft.Json;
using System;

namespace KustoTest2.Models
{
    public class PowerDeviceEvent
    {
        [JsonProperty("timestamp")]
        public DateTime TimeStamp { get; set; }
        public string DataCenterName { get; set; }
        public string DataPoint { get; set; }
        public long Status { get; set; }
        public double? Value { get; set; }
    }
}
