using KustoTest2.DocDb;
using KustoTest2.Kusto;
using KustoTest2.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly bool _clearTarget;

        public SyncKustoTableWorker(
            IKustoClient kustoClient,
            IDocDbClient docDbClient,
            ILoggerFactory loggerFactory,
            IConfiguration configuration)
        {
            _kustoClient = kustoClient;
            _docDbClient = docDbClient;
            _logger = loggerFactory.CreateLogger<SyncKustoTableWorker>();
            _clearTarget = configuration.GetValue<bool>("ClearTargetBeforeSync");
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (_clearTarget)
            {
                await ClearTarget();
            }

            var query = GenerateTableQuery("CEPowerDevices_Prod", "DeviceName", SortDirection.Asc);
            _logger.LogInformation($"running kusto query: \n{query}\n");

            await _kustoClient.ExecuteQuery<PowerDevice>(query, async (list) =>
            {
                if (list?.Any() == true)
                {
                    var totalAdded = await Ingest(list);
                    _logger.LogInformation($"total of {list.Count} raw events found, {totalAdded} mapped device events added");
                }
            }, cancellationToken);
        }

        public void Dispose()
        {
            _kustoClient?.Dispose();
        }

        private async Task ClearTarget()
        {
            await _docDbClient.ClearAll();
        }

        private string GenerateTableQuery(string tableName, string sortField, SortDirection sortDirection)
        {
            return $"{tableName} | order by {sortField} {sortDirection.ToString().ToLower()}";
        }

        private async Task<int> Ingest(IEnumerable<PowerDevice> events)
        {
            var objs = events.Select(e => (object)e).ToList();
            return await _docDbClient.UpsertObjects(objs);
        }
    }

    public enum SortDirection
    {
        Asc,
        Desc
    }
}
