using KustoTest2.Config;
using KustoTest2.DocDb;
using KustoTest2.Kusto;
using KustoTest2.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace KustoTest2
{
    /// <summary>
    /// this job does the following in batches:
    /// 1. reads raw data from kusto
    /// 2. enrich data with lookup props
    /// 3. evaluate data based on rules
    /// 4. store data in docdb
    /// </summary>
    public class SyncKustoTableWorker : IDisposable
    {
        private readonly IKustoClient _kustoClient;
        private readonly IDocDbClient _docDbClient;
        private readonly ILogger<SyncKustoTableWorker> _logger;
        private readonly KustoSettings _kustoSettings;
        private readonly JsonSerializer _jsonSerializer;

        public SyncKustoTableWorker(
            IKustoClient kustoClient,
            IDocDbClient docDbClient,
            ILoggerFactory loggerFactory,
            IConfiguration configuration)
        {
            _kustoClient = kustoClient;
            _docDbClient = docDbClient;
            _logger = loggerFactory.CreateLogger<SyncKustoTableWorker>();
            _kustoSettings = configuration.GetConfiguredSettings<KustoSettings>();
            _jsonSerializer = new JsonSerializer();
            _jsonSerializer.Converters.Add(new StringEnumConverter());
            _jsonSerializer.ContractResolver = new DefaultContractResolver()
            {
                NamingStrategy = new CamelCaseNamingStrategy()
                {
                    OverrideSpecifiedNames = false
                }
            };
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var queryFolder = Path.Combine(Directory.GetCurrentDirectory(), "KustoQueries");
            if (!Directory.Exists(queryFolder))
            {
                throw new Exception($"Query folder not found: {queryFolder}");
            }

            foreach(var syncSettings in _kustoSettings.Tables)
            {
                _logger.LogInformation($"Sync model {syncSettings.Model}...");
                var modelType = typeof(Device).Assembly.GetTypes().FirstOrDefault(t => t.Name.EndsWith(syncSettings.Model));
                if (modelType == null)
                {
                    throw new Exception($"Unable to find model type: {syncSettings.Model}");
                }

                await _docDbClient.SwitchCollection(syncSettings.DocDb, syncSettings.Collection);
                _logger.LogInformation($"set target to cosmos, db: {_docDbClient.Database.Id}, coll: {_docDbClient.Collection.Id}");
                if (syncSettings.ClearTarget)
                {
                    _logger.LogInformation($"Clearing target docdb: {_docDbClient.Database.Id}/{_docDbClient.Collection.Id}");
                    await ClearTarget();
                }

                int totalIngested = 0;
                var queryFile = Path.Combine(queryFolder, syncSettings.Query);
                if (!File.Exists(queryFile))
                {
                    throw new Exception($"Unable to find query file: {queryFile}");
                }
                var query = File.ReadAllText(queryFile);
                
                if (syncSettings.SplitByDc)
                {
                    var dcListFile = Path.Combine(queryFolder, "DC.txt");
                    var dcList = File.ReadAllText(dcListFile).Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(dc=>!string.IsNullOrWhiteSpace(dc)).ToList();
                    foreach(var dc in dcList)
                    {
                        var dcQuery = string.Format(query, dc);
                        var recordAdded = await ExecuteQuery(dcQuery, syncSettings, cancellationToken);
                        totalIngested += recordAdded;
                    }
                }
                else
                {
                    var recordAdded = await ExecuteQuery(query, syncSettings, cancellationToken);
                    totalIngested += recordAdded;
                }

                _logger.LogInformation($"total of {totalIngested} records added to docdb: {_docDbClient.Database.Id}/{_docDbClient.Collection.Id}");
            }

            _logger.LogInformation("Done!");
            Console.ReadKey();
        }

        private async Task<int> ExecuteQuery(string query, SyncSettings syncSettings, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"running kusto query: \n{query}\n");
            int totalIngested = 0;

            switch (syncSettings.Model)
            {
                case nameof(PowerDevice):
                    _logger.LogInformation($"synchronizing {nameof(PowerDevice)}");
                    await _kustoClient.ExecuteQuery<PowerDevice>(query, async (list) =>
                    {
                        if (list?.Any() == true)
                        {
                            var totalAdded = await Ingest(list);
                            totalIngested += totalAdded;
                            _logger.LogInformation($"{nameof(PowerDevice)}: total of {list.Count} raw events found, {totalAdded} mapped device events added");
                        }
                    }, cancellationToken);
                    break;
                case nameof(PowerDeviceEvent):
                    _logger.LogInformation($"synchronizing {nameof(PowerDeviceEvent)}");
                    await _kustoClient.ExecuteQuery<PowerDeviceEvent>(query, async (list) =>
                    {
                        if (list?.Any() == true)
                        {
                            var totalAdded = await Ingest(list);
                            totalIngested += totalAdded;
                            _logger.LogInformation($"{nameof(PowerDeviceEvent)}: total of {list.Count} raw events found, {totalAdded} mapped device events added");
                        }
                    }, cancellationToken);
                    break;
                case nameof(DcRank):
                    _logger.LogInformation($"synchronizing {nameof(DcRank)}");
                    await _kustoClient.ExecuteQuery<DcRank>(query, async (list) =>
                    {
                        if (list?.Any() == true)
                        {
                            var totalAdded = await Ingest(list);
                            totalIngested += totalAdded;
                            _logger.LogInformation($"{nameof(DcRank)}: total of {list.Count} raw events found, {totalAdded} mapped device events added");
                        }
                    }, cancellationToken);
                    break;
                case nameof(DeviceLocation):
                    _logger.LogInformation($"synchronizing {nameof(DeviceLocation)}");
                    await _kustoClient.ExecuteQuery<DeviceLocation>(query, async (list) =>
                    {
                        if (list?.Any() == true)
                        {
                            var totalAdded = await Ingest(list);
                            totalIngested += totalAdded;
                            _logger.LogInformation($"{nameof(DeviceLocation)}: total of {list.Count} raw events found, {totalAdded} mapped device events added");
                        }
                    }, cancellationToken);
                    break;
                case nameof(DataCenter):
                    _logger.LogInformation($"synchronizing {nameof(DataCenter)}");
                    await _kustoClient.ExecuteQuery<DataCenter>(query, async (list) =>
                    {
                        if (list?.Any() == true)
                        {
                            var totalAdded = await Ingest(list);
                            totalIngested += totalAdded;
                            _logger.LogInformation($"{nameof(DataCenter)}: total of {list.Count} raw events found, {totalAdded} mapped device events added");
                        }
                    }, cancellationToken);
                    break;
                default:
                    throw new NotSupportedException($"model {syncSettings.Model} is not supported");
            }

            return totalIngested;
        }

        public void Dispose()
        {
            _kustoClient?.Dispose();
        }

        private async Task ClearTarget()
        {
            _logger.LogInformation($"Clearing cosmos db collection, db: {_docDbClient.Database.Id}, coll: {_docDbClient.Collection.Id}");
            await _docDbClient.ClearAll();
        }

        private string GenerateTableQuery(string tableName, string sortField, SortDirection sortDirection)
        {
            return $"{tableName} | order by {sortField} {sortDirection.ToString().ToLower()}";
        }

        private async Task<int> Ingest<T>(IEnumerable<T> events)
        {
            var objs = events.Select(e => JObject.FromObject(e, _jsonSerializer)).ToList();
            return await _docDbClient.UpsertObjects(objs);
        }
    }

    public enum SortDirection
    {
        Asc,
        Desc
    }
}
