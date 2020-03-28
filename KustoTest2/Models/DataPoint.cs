using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KustoTest2.Models
{
    public class DataPoint
    {
        [JsonProperty("dataPoint")]
        public string Name { get; set; }
        public string DataType { get; set; }
        public string ChannelType { get; set; }
        public string Channel { get; set; }
        public int Offset { get; set; }
        public int PollInterval { get; set; }
        public int Scaling { get; set; }
        public string Primitive { get; set; }
        public bool FilterdOutInPG { get; set; }
    }
}
