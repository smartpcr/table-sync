//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="GraphDbRepository.cs" company="Microsoft Corporation">
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
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class GraphDbRepository<V, E> : IGraphDbRepo<V, E>
        where V : class, IGremlinVertex, new()
        where E : class, IGremlinEdge, new()
    {
        private readonly ILogger<GraphDbRepository<V, E>> logger;
        private readonly IGraphDbClient<V, E> client;

        public GraphDbRepository(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<GraphDbRepository<V, E>>();

            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var cosmosDbRepoSettings = configuration.GetConfiguredSettings<CosmosDbRepoSettings>();
            var prop = cosmosDbRepoSettings.GetType().GetProperties()
                .FirstOrDefault(p =>
                {
                    var customAttr = p.GetCustomAttribute<GraphModelBindAttribute>();
                    if (customAttr != null && customAttr.VertexType == typeof(V) && customAttr.EdgeType == typeof(E))
                    {
                        return true;
                    }

                    return false;
                });
            if (prop == null)
            {
                throw new Exception($"Missing backend mapping for model, vertex: {typeof(V).Name}, edge: {typeof(E).Name}");
            }

            var settings = prop.GetValue(cosmosDbRepoSettings) as CosmosDbSettings;
            logger.LogInformation($"initializing doc db repo: {settings.Account}/{settings.Db}/{settings.Collection}");

            client = new GraphDbClient<V, E>(
                serviceProvider,
                loggerFactory,
                new OptionsWrapper<CosmosDbSettings>(settings));
        }

        public Task<IEnumerable<V>> Query(V fromVertex, string query, CancellationToken cancel)
        {
            logger.LogInformation(query);
            throw new NotImplementedException();
        }

        public Task<IEnumerable<E>> GetPath(V fromVertex, V toVertex, CancellationToken cancel)
        {
            logger.LogInformation($"path from {fromVertex.GetId()} to {toVertex.GetId()}");
            throw new NotImplementedException();
        }

        public async Task BulkInsertVertices(IEnumerable<V> vertices, string partition, CancellationToken cancel)
        {
            await client.BulkInsertVertices(vertices, partition, cancel);
        }

        public async Task BulkInsertEdges(IEnumerable<E> edges, string partition, CancellationToken cancel)
        {
            await client.BulkInsertEdges(edges, partition, cancel);
        }
    }
}