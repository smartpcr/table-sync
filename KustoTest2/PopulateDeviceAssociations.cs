//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="PopulateDeviceAssociations.cs" company="Microsoft Corporation">
//   Copyright (C) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace KustoTest2
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Config;
    using DocDb;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;

    public class PopulateDeviceAssociations : IDisposable
    {
        private readonly ILogger<PopulateDeviceAssociations> logger;
        private readonly IDocDbClient srcDocDb;
        private readonly IDocDbClient tgtDocDb;
        private readonly JsonSerializer _jsonSerializer;

        public PopulateDeviceAssociations(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<PopulateDeviceAssociations>();
            tgtDocDb = serviceProvider.GetRequiredService<IDocDbClient>();

            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var docDbData = configuration.GetConfiguredSettings<DocDbData>();
            var prop = docDbData.GetType().GetProperties().FirstOrDefault(
                p => p.GetCustomAttribute<ModelBindAttribute>()?.ModelType == typeof(DeviceRelation));
            if (prop == null)
            {
                throw new InvalidOperationException($"Missing configuration for type {typeof(DeviceRelation).Name}");
            }
            var docDbSetting = prop.GetValue(docDbData) as DocDbSettings;

            srcDocDb = new DocDbClient(
                serviceProvider,
                loggerFactory,
                new OptionsWrapper<DocDbSettings>(docDbSetting));

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

        public async Task ExecuteAsync(CancellationToken token)
        {
            await ReadSource(srcDocDb, tgtDocDb, token);
        }

        private async Task ReadSource(IDocDbClient src, IDocDbClient tgt, CancellationToken token)
        {
            await src.Query<DeviceRelation>(
                new SqlQuerySpec("select * from c"),
                async (list, cancel) => await UpdateDeviceRelations(tgt, list, cancel),
                10000,
                new FeedOptions() { EnableCrossPartitionQuery = true, MaxItemCount = -1 },
                token);
        }

        private async Task UpdateDeviceRelations(
            IDocDbClient tgt, 
            List<DeviceRelation> relations,
            CancellationToken token)
        {
            logger.LogInformation($"saving device relations...");
            var objs = relations.Select(e => JObject.FromObject(e, _jsonSerializer)).ToList();
            await tgt.UpsertObjects(objs, token);
        }

        #region dispose
        public void Dispose()
        {
            srcDocDb?.Dispose();
            tgtDocDb?.Dispose();
        }
        #endregion
    }
}