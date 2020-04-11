//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="GraphDbClient.cs" company="Microsoft Corporation">
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
    using KV;
    using Microsoft.Azure.CosmosDB.BulkExecutor;
    using Microsoft.Azure.CosmosDB.BulkExecutor.Graph;
    using Microsoft.Azure.CosmosDB.BulkExecutor.Graph.Element;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public class GraphDbClient<V, E> : IGraphDbClient<V, E>
       where V : class, IGremlinVertex, new()
       where E : class, IGremlinEdge, new()
    {
        private readonly ILogger<GraphDbClient<V, E>> logger;
        private readonly IBulkExecutor bulkExecutor;
        private readonly List<PropertyInfo> vertexProps;

        public DocumentCollection Collection { get; }
        public DocumentClient Client { get; }

        public GraphDbClient(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IOptions<CosmosDbSettings> cosmosDbSettings)
        {
            logger = loggerFactory.CreateLogger<GraphDbClient<V, E>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var settings = cosmosDbSettings.Value ?? configuration.GetConfiguredSettings<CosmosDbSettings>();

            var kvClient = serviceProvider.GetRequiredService<IKeyVaultClient>();
            var vaultSettings = configuration.GetConfiguredSettings<VaultSettings>();
            logger.LogInformation(
                $"Retrieving auth key '{settings.AuthKeySecret}' from vault '{vaultSettings.VaultName}'");
            var authKey = kvClient.GetSecretAsync(
                vaultSettings.VaultUrl,
                settings.AuthKeySecret).GetAwaiter().GetResult();
            Client = new DocumentClient(
                settings.AccountUri,
                authKey.Value,
                desiredConsistencyLevel: ConsistencyLevel.Session,
                serializerSettings: new JsonSerializerSettings()
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });

            var database = Client.CreateDatabaseQuery().Where(db => db.Id == settings.Db).AsEnumerable().First();
            Collection = Client.CreateDocumentCollectionQuery(database.SelfLink)
                .Where(c => c.Id == settings.Collection).AsEnumerable().First();
            bulkExecutor = new GraphBulkExecutor(Client, Collection);
            logger.LogInformation($"Connected to graph db '{Collection.SelfLink}'");

            vertexProps = typeof(V).GetProperties().Where(p => p.CanRead && p.CanWrite).ToList();
        }

        public async Task BulkInsertVertices(IEnumerable<V> vertices, string partition, CancellationToken cancel)
        {
            var objs = vertices.Select(ToVertex).ToList();
            logger.LogInformation($"Adding vertices to partition {partition}...{objs.Count}");
            await bulkExecutor.BulkImportAsync(objs, true, true, null, null, cancel);
        }

        public async Task BulkInsertEdges(IEnumerable<E> edges, string partition, CancellationToken cancel)
        {
            var objs = edges.Select(ToEdge).ToList();
            logger.LogInformation($"Adding edges to partition {partition}...{objs.Count}");
            await bulkExecutor.BulkImportAsync(objs, true, true, null, null, cancel);
        }

        private GremlinVertex ToVertex(V v)
        {
            var vertex = new GremlinVertex(v.GetId(), v.GetLabel());
            foreach (var prop in vertexProps)
            {
                vertex.AddProperty(new GremlinVertexProperty(prop.Name, prop.GetValue(v)));
            }

            return vertex;
        }

        private GremlinEdge ToEdge(E e)
        {
            var outV = e.GetOutVertex();
            var inV = e.GetInVertex();

            var edge = new GremlinEdge(
                e.GetId(),
                e.GetLabel(),
                e.GetOutVertexId(),
                e.GetInVertexId(),
                outV.GetLabel(),
                inV.GetLabel(),
                outV.GetPartition(),
                inV.GetPartition());

            return edge;
        }
    }
}