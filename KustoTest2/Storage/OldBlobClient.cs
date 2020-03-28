using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KustoTest2.Aad;
using KustoTest2.Config;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace KustoTest2.Storage
{
    public class OldBlobClient : IBlobClient
    {
        private readonly ILogger<OldBlobClient> logger;
        private readonly CloudBlobClient blobClient;
        private readonly BlobStorageSettings storageSettings;

        public OldBlobClient(IConfiguration config, ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<OldBlobClient>();
            storageSettings = config.GetConfiguredSettings<BlobStorageSettings>();
            logger.LogInformation(
                $"accessing blob (account={storageSettings.Account}, container={storageSettings.Container}) using default azure credential");
            var aadSettings = config.GetConfiguredSettings<AadSettings>();
            var authBuilder = new AadAuthBuilder(aadSettings);
            var clientSecretOrCert = authBuilder.GetClientSecretOrCert();
            logger.LogInformation($"Retrieving access token for aad client: {aadSettings.ClientId}");
            var tokenCredential = GetTokenCredential(
                aadSettings.Authority,
                $"https://{storageSettings.Account}.blob.core.windows.net/",
                aadSettings.ClientId,
                clientSecretOrCert.secret).GetAwaiter().GetResult();
            StorageCredentials storageCredentials = new StorageCredentials(tokenCredential);
            blobClient = new CloudBlobClient(storageSettings.BlobEndpointUri, storageCredentials);
        }

        public Task<int> CountAsync<T>(string blobFolder, Func<T, bool> filter, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DeleteBlobs(string blobFolder, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DownloadAsync(string blobFolder, string blobName, string localFolder, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<T>> ListAsync<T>(string blobFolder, Func<string, bool> filter, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task ReleaseLease(string blobName)
        {
            throw new NotImplementedException();
        }

        public Task<IList<T>> TryAcquireLease<T>(string blobFolder, int take, Action<T> update, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public async Task UploadAsync(string blobFolder, string blobName, string blobContent, CancellationToken cancellationToken)
        {
            try
            {
                var blobPath = !string.IsNullOrEmpty(blobFolder) ? $"{blobFolder}/{blobName}" : blobName;
                logger.LogInformation($"creating {blobPath}...");
                var container = blobClient.GetContainerReference(storageSettings.Container);
                container.CreateIfNotExists();
                var blob = container.GetBlockBlobReference(blobPath);
                await blob.UploadTextAsync(blobContent, cancellationToken);
                logger.LogInformation($"uploaded blob: {blobPath}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "failed");
				throw;
            }
        }

        public Task UploadBatchAsync<T>(string blobFolder, Func<T, string> getName, IList<T> list, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task<TokenCredential> GetTokenCredential(string authorityUrl, string resourceUrl, string clientId, string clientKey)
        {
            logger.LogInformation($"building storage credential with token expiration renewal callback");

            //var tokenProvider = new AzureServiceTokenProvider();
            //var accessToken = await tokenProvider.GetAccessTokenAsync(resourceUrl);
            //return new TokenCredential(accessToken);
            var authenticationContext = new AuthenticationContext(authorityUrl);
            var state = new Tuple<AuthenticationContext, string, string, string>(authenticationContext, resourceUrl, clientId, clientKey);
            var tokenAndFrequency = await RenewTokenAsync(state, new CancellationToken());
            var tokenCredential = new TokenCredential(tokenAndFrequency.Token, RenewTokenAsync, state, tokenAndFrequency.Frequency.Value);
            return tokenCredential;
        }

        /// <summary>
        /// Renew the token
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>System.Threading.Tasks.Task&lt;Microsoft.Azure.Storage.Auth.NewTokenAndFrequency&gt;.</returns>
        private async Task<NewTokenAndFrequency> RenewTokenAsync(Object state, CancellationToken cancellationToken)
        {
            var passingState = (Tuple<AuthenticationContext, string, string, string>)state;
            AuthenticationContext authContext = passingState.Item1;
            string resourceUrl = passingState.Item2;
            var clientId = passingState.Item3;
            var clientKey = passingState.Item4;

            logger.LogInformation($"get aad access token for client {clientId} and scope: {resourceUrl}");
            var authResult = await authContext.AcquireTokenAsync(resourceUrl, new ClientCredential(clientId, clientKey));

            // Renew the token 5 minutes before it expires.
            var next = (authResult.ExpiresOn - DateTimeOffset.UtcNow) - TimeSpan.FromMinutes(5);
            if (next.Ticks < 0)
            {
                logger.LogInformation("Token expired");
                next = default(TimeSpan);
            }

            // Return the new token and the next refresh time.
            return new NewTokenAndFrequency(authResult.AccessToken, next);
        }
    }
}
