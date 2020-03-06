using Newtonsoft.Json;

namespace KustoTest2.Models
{
    public class DcRank
    {
        [JsonProperty("DcName")]
        public string DataCenterCode { get; set; }

        public string Colocation { get; set; }
        public string Row { get; set; }
        public string Rack { get; set; }
        public string Node { get; set; }
        public string NodeAssetTag { get; set; }
    }
}
