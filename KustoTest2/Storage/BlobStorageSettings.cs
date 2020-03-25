using System;

namespace KustoTest2.Storage
{
    public class BlobStorageSettings
    {
        public const string StorageResourceUrl = "https://storage.azure.com/";

        public string Account { get; set; }
        public string Container { get; set; }
        public string ConnectionStringEnvName { get; set; }
        public string ConnectionStringSecretName { get; set; }
        public Uri ContainerEndpoint => new Uri($"https://{Account}.blob.core.windows.net/{Container}");
        public Uri BlobEndpointUri => new Uri($"https://{Account}.blob.core.windows.net/");
    }
}
