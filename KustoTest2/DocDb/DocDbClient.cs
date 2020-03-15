using EnsureThat;
using KustoTest2.KV;
using Microsoft.Azure.CosmosDB.BulkExecutor;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KustoTest2.DocDb
{
    public sealed class DocDbClient : IDocDbClient
    {
        private readonly DocDbSettings _settings;
        private readonly ILogger<DocDbClient> _logger;
        private readonly FeedOptions _feedOptions;

        public Database Database { get; private set; }
        public DocumentCollection Collection { get; private set; }
        public DocumentClient Client { get; }

        public DocDbClient(
            IKeyVaultClient kvClient,
            IOptions<VaultSettings> vaultSettings,
            IOptions<DocDbSettings> dbSettings,
            ILogger<DocDbClient> logger)
        {
            _settings = dbSettings.Value;
            _logger = logger;

            _logger.LogInformation($"Retrieving auth key '{_settings.AuthKeySecret}' from vault '{vaultSettings.Value.VaultName}'");
            var authKey = kvClient.GetSecretAsync(
                vaultSettings.Value.VaultUrl,
                _settings.AuthKeySecret).GetAwaiter().GetResult();
            Client = new DocumentClient(
                _settings.AccountUri,
                authKey.Value.ToSecureString(),
                desiredConsistencyLevel: ConsistencyLevel.Session,
                serializerSettings: new JsonSerializerSettings()
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });

            Database = Client.CreateDatabaseQuery().Where(db => db.Id == _settings.Db).AsEnumerable().First();
            Collection = Client.CreateDocumentCollectionQuery(Database.SelfLink).Where(c => c.Id == _settings.Collection).AsEnumerable().First();
            _feedOptions = new FeedOptions() { PopulateQueryMetrics = _settings.CollectMetrics };

            _logger.LogInformation($"Connected to doc db '{Collection.SelfLink}'");
        }

        public async Task SwitchCollection(string dbName, string collectionName, params string[] partitionKeyPaths)
        {
            _settings.Db = dbName;
            _settings.Collection = collectionName;

            if (Collection?.Id == collectionName)
            {
                return;
            }

            Database = Client.CreateDatabaseQuery().Where(db => db.Id == _settings.Db).AsEnumerable().First();
            Collection = Client.CreateDocumentCollectionQuery(Database.SelfLink)
                .Where(c => c.Id == _settings.Collection).AsEnumerable().FirstOrDefault();
            if (Collection == null)
            {
                var partition = new PartitionKeyDefinition();
                if (partitionKeyPaths?.Any() == true)
                {
                    foreach (var keyPath in partitionKeyPaths)
                    {
                        partition.Paths.Add(keyPath);
                    }
                }
                else
                {
                    partition.Paths.Add("/id");
                }

                try
                {
                    await Client.CreateDocumentCollectionAsync(Database.SelfLink, new DocumentCollection()
                    {
                        Id = collectionName,
                        PartitionKey = partition
                    }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create collection");
                    throw;
                }

                Collection = Client.CreateDocumentCollectionQuery(Database.SelfLink).Where(c => c.Id == collectionName).AsEnumerable().FirstOrDefault();
                _logger.LogInformation("Created collection {0} in {1}/{2}", collectionName, _settings.Account, _settings.Db);
            }

            _logger.LogInformation("Switched to collection {0}", collectionName);
        }

        public async Task<int> CountAsync(CancellationToken cancel = default)
        {
            var countQuery = @"SELECT VALUE COUNT(1) FROM c";
            var result = Client.CreateDocumentQuery<int>(
                Collection.DocumentsLink,
                new SqlQuerySpec()
                {
                    QueryText = countQuery,
                    Parameters = new SqlParameterCollection()
                },
                new FeedOptions()
                {
                    EnableCrossPartitionQuery = true
                }).AsDocumentQuery();

            int count = 0;
            while (result.HasMoreResults)
            {
                var batchSize = await result.ExecuteNextAsync<int>(cancel);
                count += batchSize.First();
            }
            return count;
        }

        public async Task DeleteObject(string id, CancellationToken cancel = default)
        {
            Ensure.That(id).IsNotNullOrWhiteSpace();

            try
            {
                Uri docUri = UriFactory.CreateDocumentUri(_settings.Db, _settings.Collection, id);
                await Client.DeleteDocumentAsync(docUri, cancellationToken: cancel);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to Delete document. DatabaseName={0}, CollectionName={1}, DocumentId={2}, Exception={3}",
                    _settings.Db, _settings.Collection, id);
                throw;
            }
        }

        public async Task<int> UpsertObjects(List<object> list, CancellationToken cancel = default)
        {
            Client.ConnectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = 30;
            Client.ConnectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 9;
            IBulkExecutor bulkExecutor = new BulkExecutor(Client, Collection);
            await bulkExecutor.InitializeAsync();
            Client.ConnectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = 0;
            Client.ConnectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 0;

            var response = await bulkExecutor.BulkImportAsync(
                list,
                enableUpsert: true,
                disableAutomaticIdGeneration: false,
                cancellationToken: cancel);

            _logger.LogInformation($"Wrote {response.NumberOfDocumentsImported} documents");

            _logger.LogInformation(
                $"Total of {response.NumberOfDocumentsImported} documents written to {Collection.Id}.");

            return (int)response.NumberOfDocumentsImported;
        }

        public async Task<IEnumerable<T>> Query<T>(SqlQuerySpec querySpec, FeedOptions feedOptions = null, CancellationToken cancel = default)
        {
            Ensure.That(querySpec).IsNotNull();

            try
            {
                var output = new List<T>();
                feedOptions = feedOptions ?? _feedOptions;
                feedOptions.PopulateQueryMetrics = _feedOptions.PopulateQueryMetrics;
                var query = Client
                    .CreateDocumentQuery<T>(Collection.SelfLink, querySpec, feedOptions)
                    .AsDocumentQuery();

                while (query.HasMoreResults)
                {
                    var response = await query.ExecuteNextAsync<T>(cancel);
                    output.AddRange(response);

                    if (_settings.CollectMetrics)
                    {
                        var queryMetrics = response.QueryMetrics;
                        foreach (var label in queryMetrics.Keys)
                        {
                            var queryMetric = queryMetrics[label];
                        }
                    }
                }
                //_logger.LogInformation("Total RU for {0}: {1}", nameof(Query), totalRequestUnits);

                return output;
            }
            catch (DocumentClientException e)
            {
                _logger.LogError(
                    e,
                    $"Unable to Query. DatabaseName={_settings.Db}, CollectionName={_settings.Collection}, Query={querySpec}, FeedOptions={feedOptions}");

                throw;
            }

        }

        public async Task<FeedResponse<T>> QueryInBatches<T>(SqlQuerySpec querySpec, FeedOptions feedOptions = null,
            CancellationToken cancel = default)
        {
            Ensure.That(querySpec).IsNotNull();

            try
            {
                feedOptions = feedOptions ?? _feedOptions;
                feedOptions.PopulateQueryMetrics = _feedOptions.PopulateQueryMetrics;
                var query = Client
                    .CreateDocumentQuery<T>(Collection.SelfLink, querySpec, feedOptions)
                    .AsDocumentQuery();

                if (query.HasMoreResults)
                {
                    var response = await query.ExecuteNextAsync<T>(cancel);
                    return response;
                }

                return null;
            }
            catch (DocumentClientException e)
            {
                _logger.LogError(
                    e,
                    $"Unable to Query. DatabaseName={_settings.Db}, CollectionName={_settings.Collection}, Query={querySpec}, FeedOptions={feedOptions}");

                throw;
            }
        }

        public async Task<T> ReadObject<T>(string id, CancellationToken cancel = default)
        {
            try
            {
                Uri docUri = UriFactory.CreateDocumentUri(_settings.Db, _settings.Collection, id);
                var response = await Client.ReadDocumentAsync<T>(docUri, cancellationToken: cancel);

                return response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to Read document. DatabaseName={0}, CollectionName={1}, DocumentId={2}, Exception={3}",
                    _settings.Db, _settings.Collection, id);
                throw;
            }
        }

        public async Task<string> UpsertObject<T>(T @object, RequestOptions requestOptions = null, CancellationToken cancel = default)
        {
            try
            {
                var response = await Client.UpsertDocumentAsync(Collection.SelfLink, @object, requestOptions, cancellationToken: cancel);

                return response.Resource.Id;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to Upsert object. CollectionUrl={0}",
                    Collection.SelfLink);
                throw;
            }
        }

        public async Task ClearAll(CancellationToken cancel = default)
        {
            // this is slow, use sp to do bulk delete
            try
            {
                var idsToDelete = new List<string>();
                var feedOptions = new FeedOptions() { EnableCrossPartitionQuery = true };

                StoredProcedure bulkDeleteSp = Client.CreateStoredProcedureQuery(Collection.SelfLink)
                    .AsEnumerable().FirstOrDefault(sp => sp.Id == "bulkDelete");
                if (bulkDeleteSp == null)
                {
                    throw new Exception("stored procedure with name 'bulkDelete' need to be deployed");
                }

                var response = await Client.ExecuteStoredProcedureAsync<string>(
                    bulkDeleteSp.SelfLink,
                    new RequestOptions() { PartitionKey = new PartitionKey(Undefined.Value) });
                int totalDeleted = 0;
                var jsonObj = JObject.Parse(response);
                var deleted = jsonObj.Value<int>("deleted");
                totalDeleted += deleted;
                var continueation = jsonObj.Value<bool>("continuation");
                while (continueation)
                {
                    _logger.LogInformation($"deleting...{totalDeleted}");

                    response = await Client.ExecuteStoredProcedureAsync<string>(
                        bulkDeleteSp.SelfLink,
                        new RequestOptions() { PartitionKey = new PartitionKey(Undefined.Value) });
                    jsonObj = JObject.Parse(response);
                    deleted = jsonObj.Value<int>("deleted");
                    totalDeleted += deleted;
                    continueation = jsonObj.Value<bool>("continuation");
                }
                _logger.LogInformation($"total of {totalDeleted} records are deleted from collection: {Collection.Id}");
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    $"Unable to clear collection. DatabaseName={_settings.Db}, CollectionName={_settings.Collection}");

                throw;
            }
        }

        #region IDisposable Support
        private bool isDisposed; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    Client.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                isDisposed = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DocDb() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }

    internal static class StringExtension
    {
        public static SecureString ToSecureString(this string input)
        {
            char[] chars = input.ToCharArray();
            SecureString secureString = new SecureString();
            foreach (char ch in chars)
            {
                secureString.AppendChar(ch);
            }
            return secureString;
        }
    }
}
