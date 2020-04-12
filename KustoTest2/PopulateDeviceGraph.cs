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
    using System.Threading;
    using System.Threading.Tasks;
    using DocDb;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Models;

    public class PopulateDeviceGraph : IDisposable
    {
        private readonly ILogger<PopulateDeviceGraph> logger;
        private readonly IDocDbRepo<DataCenter> dataCenterRepo;
        private readonly IDocDbRepo<PowerDevice> powerDeviceRepo;
        private readonly IDocDbRepo<DeviceRelation> deviceRelationRepo;
        private readonly IGraphDbRepo<PowerDeviceNode, PowerDeviceEdge> graphRepo;

        public PopulateDeviceGraph(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<PopulateDeviceGraph>();
            var repoFactory = serviceProvider.GetRequiredService<CosmosDbRepoFactory>();
            dataCenterRepo = repoFactory.CreateRepository<DataCenter>();
            powerDeviceRepo = repoFactory.CreateRepository<PowerDevice>();
            deviceRelationRepo = repoFactory.CreateRepository<DeviceRelation>();
            graphRepo = repoFactory.CreateGraphRepository<PowerDeviceNode, PowerDeviceEdge>();
        }

        public async Task ExecuteAsync(CancellationToken cancel)
        {
            logger.LogInformation("retrieving all data centers...");
            var dcNames = await GetAllDataCenters(cancel);
            bool shouldStart = false;
            string startFromDcName = "AMS20";
            foreach (var dcName in dcNames)
            {
                if (!shouldStart && dcName.Equals(startFromDcName, StringComparison.OrdinalIgnoreCase))
                {
                    shouldStart = true;
                }

                if (!shouldStart)
                {
                    continue;
                }

                logger.LogInformation($"building graph for dc: {dcName}");
                var powerDevices = await GetPowerDevices(dcName, cancel);
                logger.LogInformation($"total of {powerDevices} devices retrieved");
                var relations = await GetDeviceRelations(dcName, cancel);
                logger.LogInformation($"total of {relations.Count} relations retrieved");

                if (powerDevices.Count > 0 && relations.Count > 0)
                {
                    logger.LogInformation("building graph...");
                    var graph = BuildGraph(powerDevices, relations);
                    logger.LogInformation($"graph: vertices={graph.vertices.Count}, edges={graph.edges.Count}");
                    await graphRepo.BulkInsertVertices(graph.vertices, dcName, cancel);
                    await graphRepo.BulkInsertEdges(graph.edges, dcName, cancel);
                }
            }
        }

        private async Task<List<string>> GetAllDataCenters(CancellationToken cancel)
        {
            var dataCenters = await dataCenterRepo.QueryAll(cancel);
            return dataCenters.Select(dc => dc.DcShortName).OrderBy(dc => dc).ToList();
        }

        private async Task<List<PowerDevice>> GetPowerDevices(string dcName, CancellationToken cancel)
        {
            var powerDevices = await powerDeviceRepo.Query($"c.dcName = '{dcName}'", cancel);
            return powerDevices;
        }

        private async Task<List<DeviceRelation>> GetDeviceRelations(string dcName, CancellationToken cancel)
        {
            var relations = await deviceRelationRepo.Query($"c.dcName='{dcName}'", cancel);
            return relations;
        }

        private (List<PowerDeviceNode> vertices, List<PowerDeviceEdge> edges) BuildGraph(
            List<PowerDevice> devices,
            List<DeviceRelation> relations)
        {
            var parentLookup = relations.Where(r => !string.IsNullOrEmpty(r.Name))
                .GroupBy(r => r.Name).ToDictionary(
                    g => g.Key,
                    g => g.ToList().Where(r => r.DirectUpstreamDeviceList != null)
                        .SelectMany(r => r.DirectUpstreamDeviceList).ToList());
            var vertices = devices.Select(d => new PowerDeviceNode() {Device = d}).ToList();
            var vertexLookup = vertices.ToDictionary(d => d.Id);

            var edges = new List<PowerDeviceEdge>();
            foreach (var device in devices)
            {
                var current = vertexLookup[device.DeviceName];

                if (parentLookup.ContainsKey(device.DeviceName))
                {
                    var relationsForDevice = parentLookup[device.DeviceName];
                    foreach (var relation in relationsForDevice)
                    {
                        if (vertexLookup.ContainsKey(relation.DeviceName))
                        {
                            var parent = vertexLookup[relation.DeviceName];
                            edges.Add(new PowerDeviceEdge()
                            {
                                Association = relation.AssociationType,
                                From = current,
                                To = parent
                            });
                        }
                    }
                }
            }

            return (vertices, edges);
        }

        #region dispose
        private void ReleaseUnmanagedResources()
        {
            powerDeviceRepo?.Dispose();
            deviceRelationRepo?.Dispose();
            graphRepo?.Dispose();
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