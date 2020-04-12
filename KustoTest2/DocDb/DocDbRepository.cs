//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="DocDbRepository.cs" company="Microsoft Corporation">
//   Copyright (C) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace KustoTest2.DocDb
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Config;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;

    public class DocDbRepository<T> : IDocDbRepo<T> where T: class, new()
    {
        private readonly IDocDbClient docDbClient;
        private readonly ILogger<DocDbRepository<T>> logger;
        private readonly JsonSerializer jsonSerializer;

        public DocDbRepository(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<DocDbRepository<T>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var cosmosDbRepoSettings = configuration.GetConfiguredSettings<CosmosDbRepoSettings>();
            var prop = cosmosDbRepoSettings.GetType().GetProperties()
                .FirstOrDefault(p =>
                {
                    var customAttr = p.GetCustomAttribute<ModelBindAttribute>();
                    if (customAttr != null && customAttr.ModelType == typeof(T))
                    {
                        return true;
                    }

                    return false;
                });
            if (prop == null)
            {
                throw new Exception($"Missing backend mapping for model: {typeof(T).Name}");
            }
            var settings = prop.GetValue(cosmosDbRepoSettings) as CosmosDbSettings;
            logger.LogInformation($"initializing doc db repo: {settings.Account}/{settings.Db}/{settings.Collection}");

            docDbClient = new DocDbClient(
                serviceProvider,
                loggerFactory,
                new OptionsWrapper<CosmosDbSettings>(settings));

            jsonSerializer = new JsonSerializer();
            jsonSerializer.Converters.Add(new StringEnumConverter());
            jsonSerializer.ContractResolver = new DefaultContractResolver()
            {
                NamingStrategy = new CamelCaseNamingStrategy()
                {
                    OverrideSpecifiedNames = false
                }
            };
        }

        public async Task<List<T>> QueryAll(CancellationToken cancel)
        {
            var query = "select * from c";
            return await ExecuteQuery(query, cancel);
        }

        public async Task<List<T>> Query(string whereClause, CancellationToken cancel)
        {
            var query = $"select * from c where {whereClause}";
            return await ExecuteQuery(query, cancel);
        }

        public async Task Query(
            SqlQuerySpec querySpec, 
            Func<List<T>, CancellationToken, Task> onReceived, 
            int batchSize = 1000, 
            FeedOptions feedOptions = null,
            CancellationToken cancel = default)
        {
            logger.LogInformation($"query: {querySpec.QueryText}");
            await docDbClient.Query(querySpec, onReceived, batchSize, feedOptions, cancel);
        }

        public async Task<int> UpsertObjects(List<T> list, CancellationToken cancel = default)
        {
            var objs = list.Select(item => JObject.FromObject(item, jsonSerializer)).ToList();
            return await docDbClient.UpsertObjects(objs, cancel);
        }

        private async Task<List<T>> ExecuteQuery(string query, CancellationToken cancel)
        {
            logger.LogInformation($"query: {query}");
            var items = await docDbClient.Query<T>(
                new SqlQuerySpec(query),
                new FeedOptions() { EnableCrossPartitionQuery = true },
                cancel);
            var list = items.ToList();
            logger.LogInformation($"total of {list.Count} retrieved from QueryAll");
            return list;
        }

        public void Dispose()
        {
            docDbClient?.Dispose();
        }
    }
}