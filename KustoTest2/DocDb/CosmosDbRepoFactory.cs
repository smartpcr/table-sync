//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="CosmosDbRepoFactory.cs" company="Microsoft Corporation">
//   Copyright (C) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace KustoTest2.DocDb
{
    using System;
    using System.Collections.Concurrent;
    using Microsoft.Extensions.Logging;

    public class CosmosDbRepoFactory
    {
        private readonly ILogger<CosmosDbRepoFactory> logger;
        private readonly ILoggerFactory loggerFactory;
        private readonly IServiceProvider serviceProvider;
        private readonly ConcurrentDictionary<string, object> docDbRepos = new ConcurrentDictionary<string, object>();
        private readonly ConcurrentDictionary<string, object> graphDbRepos = new ConcurrentDictionary<string, object>();

        public CosmosDbRepoFactory(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
            this.serviceProvider = serviceProvider;
            logger = loggerFactory.CreateLogger<CosmosDbRepoFactory>();
        }

        public IDocDbRepo<T> CreateRepository<T>() where T : class, new()
        {
            if (docDbRepos.TryGetValue(typeof(T).Name, out var found) && found is IDocDbRepo<T> repo)
            {
                return repo;
            }

            logger.LogInformation($"creating doc db repo for {typeof(T).Name}");
            IDocDbRepo<T> repository = new DocDbRepository<T>(serviceProvider, loggerFactory);
            docDbRepos.AddOrUpdate(typeof(T).Name, repository, (k, v) => repository);

            return repository;
        }

        public IGraphDbRepo<V, E> CreateGraphRepository<V, E>()
            where V : class, IGremlinVertex, new()
            where E : class, IGremlinEdge, new()
        {
            var key = $"{typeof(V).Name}-{typeof(E).Name}";
            if (graphDbRepos.TryGetValue(key, out var found) && found is IGraphDbRepo<V,E> repo)
            {
                return repo;
            }

            logger.LogInformation($"creating doc db repo for {key}");
            IGraphDbRepo<V,E> repository = new GraphDbRepository<V, E>(serviceProvider, loggerFactory);
            docDbRepos.AddOrUpdate(key, repository, (k, v) => repository);

            return repository;
        }
    }
}