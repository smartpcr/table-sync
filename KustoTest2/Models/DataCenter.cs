﻿using Newtonsoft.Json;

namespace KustoTest2.Models
{
    public class DataCenter
    {
        [JsonProperty("DcName")]
        public string DcShortName { get; set; }

        [JsonProperty("DcLongName")]
        public string DcName { get; set; }
        
        public string Region { get; set; }
        public string CampusName { get; set; }
        public string Owner { get; set; }
        public string Class { get; set; }
        public string PhaseName { get; set; }
        public string CoolingType { get; set; }
        public string HVACType { get; set; }
        public string MSAssetId { get; set; }
    }
}