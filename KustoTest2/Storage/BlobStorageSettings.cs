using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KustoTest2.Storage
{
    public class BlobStorageSettings
    {
        public string Account { get; set; }
        public string Container { get; set; }
        public string ConnectionStringEnvName { get; set; }
        public string ConnectionStringSecretName { get; set; }
        public string ContainerEndpoint => $"https://{Account}.blob.core.windows.net/{Container}";
    }
}
