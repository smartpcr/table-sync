using Azure.Identity;
using Azure.Storage.Blobs;
using KustoTest2.Aad;
using KustoTest2.Config;
using KustoTest2.KV;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KustoTest2.Storage
{
    internal class BlobContainerFactory
    {
        private readonly BlobStorageSettings _blobSettings;
        private readonly AadSettings _aadSettings;
        private readonly VaultSettings _vaultSettings;
        private readonly ILogger<BlobContainerFactory> _logger;
        public BlobContainerClient Client { get; private set; }

        public BlobContainerFactory(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _blobSettings = configuration.GetConfiguredSettings<BlobStorageSettings>();
            _aadSettings = configuration.GetConfiguredSettings<AadSettings>();
            _vaultSettings = configuration.GetConfiguredSettings<VaultSettings>();
            _logger = loggerFactory.CreateLogger<BlobContainerFactory>();

            if (!TryCreateUsingMsi())
            {
                if (!TryCreateUsingSpn())
                {
                    if (!TryCreateFromKeyVault())
                    {
                        TryCreateUsingConnStr();
                    }
                }
            }
        }

        /// <summary>
        /// running app/svc/pod/vm is assigned an identity (user-assigned, system-assigned)
        /// </summary>
        /// <returns></returns>
        private bool TryCreateUsingMsi()
        {
            _logger.LogInformation($"trying to access blob using msi...");
            try
            {
                var containerClient = new BlobContainerClient(new Uri(_blobSettings.ContainerEndpoint), new DefaultAzureCredential());
                containerClient.CreateIfNotExists();

                TryRecreateTestBlob(containerClient);
                _logger.LogInformation($"Succeed to access blob using msi");
                Client = containerClient;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"failed to access blob {_blobSettings.Account}/{_blobSettings.Container} using msi");
                return false;
            }
        }

        /// <summary>
        /// using pre-configured spn to access storage, secret must be provided for spn authentication
        /// </summary>
        /// <returns></returns>
        private bool TryCreateUsingSpn()
        {
            _logger.LogInformation($"trying to access blob using spn...");
            try
            {
                var authBuilder = new AadAuthBuilder(_aadSettings);
                var accessToken = authBuilder.GetAccessTokenAsync("https://storage.azure.com/").GetAwaiter().GetResult();
                var tokenCredential = new ClientSecretCredential(_aadSettings.TenantId, _aadSettings.ClientId, accessToken);
                var containerClient = new BlobContainerClient(new Uri(_blobSettings.ContainerEndpoint), tokenCredential);
                containerClient.CreateIfNotExists();

                TryRecreateTestBlob(containerClient);
                _logger.LogInformation($"Succeed to access blob using msi");
                Client = containerClient;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"faield to access blob using spn...");
                return false;
            }
        }

        /// <summary>
        /// using pre-configured spn to access key vault, then retrieve sas/conn string for storage
        /// </summary>
        /// <returns></returns>
        private bool TryCreateFromKeyVault()
        {
            if (!string.IsNullOrEmpty(_blobSettings.ConnectionStringSecretName))
            {
                _logger.LogInformation($"trying to access blob from kv...");
                try
                {
                    var authBuilder = new AadAuthBuilder(_aadSettings);

                    Task<string> AuthCallback(string authority, string resource, string scope) =>
                        authBuilder.GetAccessTokenAsync(resource);

                    var kvClient = new KeyVaultClient(AuthCallback);
                    var connStrSecret = kvClient
                        .GetSecretAsync(_vaultSettings.VaultUrl, _blobSettings.ConnectionStringSecretName).Result;
                    var containerClient = new BlobContainerClient(connStrSecret.Value, _blobSettings.Container);
                    containerClient.CreateIfNotExists();

                    TryRecreateTestBlob(containerClient);
                    _logger.LogInformation($"Succeed to access blob using msi");
                    Client = containerClient;
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"faield to access blob from kv...");
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// connection string is provided as env variable (most unsecure)
        /// </summary>
        /// <returns></returns>
        private bool TryCreateUsingConnStr()
        {
            if (!string.IsNullOrEmpty(_blobSettings.ConnectionStringEnvName))
            {
                _logger.LogInformation($"trying to access blob using connection string...");
                try
                {
                    var storageConnectionString =
                        Environment.GetEnvironmentVariable(_blobSettings.ConnectionStringEnvName);
                    if (!string.IsNullOrEmpty(storageConnectionString))
                    {
                        var containerClient = new BlobContainerClient(storageConnectionString, _blobSettings.Container);
                        containerClient.CreateIfNotExists();
                        TryRecreateTestBlob(containerClient);
                        Client = containerClient;
                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"trying to access blob using connection string...");
                    return false;
                }
            }

            return false;
        }

        private void TryRecreateTestBlob(BlobContainerClient containerClient)
        {
            var blobClient = containerClient.GetBlobClient("__test");
            if (blobClient.Exists())
            {
                blobClient.Delete();
            }

            var blobContent = "test";
            blobClient.Upload(new MemoryStream(Encoding.UTF8.GetBytes(blobContent)));
        }
    }
}
