using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using KustoTest2.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KustoTest2.Storage
{
    public class BlobClient : IBlobClient
    {
        private readonly ILogger<BlobClient> _logger;
        private readonly BlobContainerClient _containerClient;

        public BlobClient(
            IConfiguration config,
            ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<BlobClient>();
            var settings = config.GetConfiguredSettings<BlobStorageSettings>();
            _logger.LogInformation(
                $"accessing blob (account={settings.Account}, container={settings.Container}) using default azure credential");
            var factory = new BlobContainerFactory(config, loggerFactory);
            _containerClient = factory.Client;
            if (_containerClient == null)
            {
                var error = $"failed to create blobContainerClient: {settings.Account}/{settings.Container}";
                _logger.LogError(error);
                throw new Exception(error);
            }
        }

        public async Task UploadAsync(string blobFolder, string blobName, string blobContent,
            CancellationToken cancellationToken)
        {
            var blobPath = $"{blobFolder}/{blobName}";
            _logger.LogInformation($"uploading {blobPath}...");
            var blobClient = _containerClient.GetBlobClient(blobPath);
            var uploadResponse = await blobClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(blobContent)),
                cancellationToken);
            _logger.LogInformation($"uploaded blob: {blobPath}, modified on: {uploadResponse.Value.LastModified}");
        }

        public async Task UploadBatchAsync<T>(string blobFolder, Func<T, string> getName, IList<T> list,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation($"uploading {list.Count} files to {blobFolder}...");
            foreach (var item in list)
            {
                var blobName = getName(item);
                var blobPath = $"{blobFolder}/{blobName}";
                var blobClient = _containerClient.GetBlobClient(blobPath);
                var blobContent = JsonConvert.SerializeObject(item);
                await blobClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(blobContent)), cancellationToken);
            }

            _logger.LogInformation($"uploaded {list.Count} files to {blobFolder}.");
        }

        public async Task DownloadAsync(string blobFolder, string blobName, string localFolder, CancellationToken cancellationToken)
        {
            var blobPath = $"{blobFolder}/{blobName}";
            _logger.LogInformation($"downloading {blobPath}...");
            var blobClient = _containerClient.GetBlobClient(blobPath);
            var downloadInfo = await blobClient.DownloadAsync(cancellationToken);
            var filePath = Path.Combine(localFolder, blobName);
            using (var fs = File.OpenWrite(filePath))
            {
                await downloadInfo.Value.Content.CopyToAsync(fs, 81920, cancellationToken);
                _logger.LogInformation($"blob written to {filePath}");
            }
        }

        public async Task<List<T>> ListAsync<T>(string blobFolder, Func<string, bool> filter, CancellationToken cancellationToken)
        {
            var blobs = _containerClient.GetBlobsAsync(prefix: blobFolder).GetAsyncEnumerator(cancellationToken);
            var output = new List<T>();
            while (await blobs.MoveNextAsync())
            {
                if (filter == null || filter(blobs.Current.Name))
                {
                    var blobClient = _containerClient.GetBlobClient(blobs.Current.Name);
                    var blobContent = await blobClient.DownloadAsync(cancellationToken);
                    using (var reader = new StreamReader(blobContent.Value.Content))
                    {
                        var json = reader.ReadToEnd();
                        output.Add(JsonConvert.DeserializeObject<T>(json));
                    }
                }
            }

            return output;
        }

        public async Task<int> CountAsync<T>(string blobFolder, Func<T, bool> filter, CancellationToken cancellationToken)
        {
            var blobs = _containerClient.GetBlobsAsync(prefix: blobFolder).GetAsyncEnumerator(cancellationToken);
            var count = 0;
            while (await blobs.MoveNextAsync())
            {
                if (filter == null)
                {
                    count++;
                }
                else
                {
                    var blobClient = _containerClient.GetBlobClient(blobs.Current.Name);
                    var blobContent = await blobClient.DownloadAsync(cancellationToken);
                    using (var reader = new StreamReader(blobContent.Value.Content))
                    {
                        var json = reader.ReadToEnd();
                        var item = JsonConvert.DeserializeObject<T>(json);
                        if (filter(item))
                        {
                            count++;
                        }
                    }
                }
            }

            return count;
        }

        public async Task<IList<T>> TryAcquireLease<T>(string blobFolder, int take, Action<T> update, TimeSpan timeout)
        {
            _logger.LogInformation($"trying to acquire lease on blobs in folder: {blobFolder}...");
            var blobs = _containerClient.GetBlobsAsync(prefix: blobFolder).GetAsyncEnumerator();

            timeout = timeout == default(TimeSpan) ? TimeSpan.FromMinutes(5) : timeout;
            var output = new Dictionary<string, T>();
            try
            {
                while (await blobs.MoveNextAsync())
                {
                    var blobClient = _containerClient.GetBlobClient(blobs.Current.Name);
                    var leaseClient = blobClient.GetBlobLeaseClient();
                    await leaseClient.AcquireAsync(timeout);
                    var blobContent = await blobClient.DownloadAsync();
                    using (var reader = new StreamReader(blobContent.Value.Content))
                    {
                        var json = reader.ReadToEnd();
                        var item = JsonConvert.DeserializeObject<T>(json);
                        if (update != null)
                        {
                            update(item);
                            await blobClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item))), new CancellationToken());
                        }
                        output.Add(blobs.Current.Name, item);
                    }

                    if (take == 0 || output.Count >= take)
                    {
                        break;
                    }
                }

                return output.Values.ToList();
            }
            catch
            {
                _logger.LogInformation($"lease rejected on blobs within folder: {blobFolder}...");
                foreach (var blobName in output.Keys)
                {
                    await ReleaseLease(blobName);
                }
                return new List<T>();
            }
        }

        public async Task ReleaseLease(string blobName)
        {
            _logger.LogInformation($"trying to acquire lease on blob: {blobName}...");
            var blobClient = _containerClient.GetBlobClient(blobName);
            var leaseClient = blobClient.GetBlobLeaseClient();
            await leaseClient.ReleaseAsync();
        }

        public async Task DeleteBlobs(string blobFolder, CancellationToken cancellationToken)
        {
            var blobs = _containerClient.GetBlobsAsync(prefix: blobFolder).GetAsyncEnumerator(cancellationToken);
            var blobNames = new List<string>();
            while (await blobs.MoveNextAsync())
            {
                blobNames.Add(blobs.Current.Name);
            }

            foreach (var blobName in blobNames)
            {
                var blobClient = _containerClient.GetBlobClient(blobName);
                await blobClient.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
            }
        }
    }
}
