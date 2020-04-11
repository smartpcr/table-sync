using System;

namespace KustoTest2.DocDb
{
    public class CosmosDbSettings
    {
        public Api Api { get; set; } = Api.SQL;
        public string Account { get; set; }
        public string Db { get; set; }
        public string Collection { get; set; }
        public string AuthKeySecret { get; set; }
        public bool CollectMetrics { get; set; }
        public Uri AccountUri => new Uri($"https://{Account}.documents.azure.com:443/");
    }

    public enum Api
    {
        SQL,
        Gremlin
    }
}
