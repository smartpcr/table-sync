//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="PopulateDeviceGraph.cs" company="Microsoft Corporation">
//   Copyright (C) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace KustoTest2
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Config;
    using DocDb;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;

    public class PopulateDeviceGraph : IDisposable
    {
        private readonly ILogger<PopulateDeviceGraph> logger;
        private readonly IDocDbRepo<PowerDevice> powerDeviceRepo;
        private readonly IDocDbRepo<DeviceRelation> deviceRelationRepo;
        private readonly IGraphDbClient<PowerDeviceNode, PowerDeviceEdge> graphClient;

        public PopulateDeviceGraph(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<PopulateDeviceGraph>();
            var repoFactory = serviceProvider.GetRequiredService<CosmosDbRepoFactory>();
            powerDeviceRepo = repoFactory.CreateRepository<PowerDevice>();
            deviceRelationRepo = repoFactory.CreateRepository<DeviceRelation>();
        }

        #region dispose
        private void ReleaseUnmanagedResources()
        {
            // TODO release unmanaged resources here
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~PopulateDeviceGraph()
        {
            ReleaseUnmanagedResources();
        }
        #endregion
    }
}