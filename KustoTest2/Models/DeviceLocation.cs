using Newtonsoft.Json;

namespace KustoTest2.Models
{
    public class DeviceLocation
    {
        [JsonProperty("id")]
        public string DeviceName { get; set; }
        public string DcName { get; set; }
        public long DcCode { get; set; }
        public string DeviceId { get; set; }
        public string ColoName { get; set; }
        public string DeviceType { get; set; }
        public string[] Racks { get; set; }
    }
}
