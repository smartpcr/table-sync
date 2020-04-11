using Newtonsoft.Json;

namespace KustoTest2.Models
{
    public class DataCenter
    {
        [JsonProperty("dcName")]
        public string DcShortName { get; set; }

        [JsonProperty("dcLongName")]
        public string DcName { get; set; }
        
        public string Region { get; set; }
        public string CampusName { get; set; }
        public string Owner { get; set; }
        public string Class { get; set; }
        public string PhaseName { get; set; }
        public string CoolingType { get; set; }
        public string HVACType { get; set; }
        public double MSAssetID { get; set; }
        public string DcGeneration { get; set; }
    }
}
