using Kusto.Cloud.Platform.Data;
using Kusto.Data.Common;
using KustoTest2.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace KustoTest2.Kusto
{
    public class KustoClient : IKustoClient
    {
        private readonly ILogger<KustoClient> _logger;
        private readonly ICslAdminProvider _adminClient;
        private readonly ICslQueryProvider _queryClient;
        private readonly KustoSettings _kustoSettings;

        public KustoClient(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<KustoClient>();
            _kustoSettings = configuration.GetConfiguredSettings<KustoSettings>();
            var clientFactory = new ClientFactory(configuration, loggerFactory);
            _queryClient = clientFactory.QueryClient;
            _adminClient = clientFactory.AdminClient;
        }

        public async Task<IEnumerable<T>> ExecuteQuery<T>(string query)
        {
            var reader = await _queryClient.ExecuteQueryAsync(
                _kustoSettings.DbName,
                query,
                new ClientRequestProperties() { ClientRequestId = Guid.NewGuid().ToString() });
            return Read<T>(reader);
        }

        public async Task ExecuteQuery<T>(
            string query,
            Func<IList<T>, Task> onBatchReceived,
            CancellationToken cancellationToken = default,
            int batchSize = 1000)
        {
            var reader = await _queryClient.ExecuteQueryAsync(
                _kustoSettings.DbName,
                query,
                new ClientRequestProperties() { ClientRequestId = Guid.NewGuid().ToString() });
            await Read<T>(reader, onBatchReceived, cancellationToken, batchSize);
        }

        public async Task<IEnumerable<T>> ExecuteFunction<T>(string functionName, params (string name, string value)[] parameters)
        {
            var functionParameters = parameters.Select(p => new KeyValuePair<string, string>(p.name, p.value));
            var reader = await _queryClient.ExecuteQueryAsync(
                _kustoSettings.DbName,
                functionName,
                new ClientRequestProperties(null, functionParameters) { ClientRequestId = Guid.NewGuid().ToString() });
            return Read<T>(reader);
        }

        public async Task ExecuteFunction<T>(string functionName, (string name, string value)[] parameters, Func<IList<T>, Task> onBatchReceived,
            CancellationToken cancellationToken = default, int batchSize = 1000)
        {
            var functionParameters = parameters.Select(p => new KeyValuePair<string, string>(p.name, p.value));
            var reader = await _queryClient.ExecuteQueryAsync(
                _kustoSettings.DbName,
                functionName,
                new ClientRequestProperties(null, functionParameters) { ClientRequestId = Guid.NewGuid().ToString() });
            await Read<T>(reader, onBatchReceived, cancellationToken, batchSize);
        }

        private IEnumerable<T> Read<T>(IDataReader reader)
        {
            var objReader = new ObjectReader<T>(reader, true, true);
            using (var enumerator = objReader.GetEnumerator())
            {
                var output = new List<T>();
                while (enumerator.MoveNext())
                {
                    output.Add(enumerator.Current);
                }
                _logger.LogInformation($"total of {output.Count} records retrieved from kusto");
                return output;
            }
        }

        private async Task Read<T>(IDataReader reader, Func<IList<T>, Task> onBatchReceived, CancellationToken cancellationToken, int batchSize)
        {
            var propMappings = BuildFieldMapping<T>(reader);

            var output = new List<T>();
            int batchCount = 0;
            int total = 0;
            while (reader.Read() && !cancellationToken.IsCancellationRequested)
            {
                var instance = Create<T>(reader, propMappings);
                output.Add(instance);
                if (output.Count >= batchSize)
                {
                    batchCount++;
                    total += output.Count;
                    await onBatchReceived(output);
                    _logger.LogInformation($"sending batch #{batchCount}, total: {total} records");
                    output = new List<T>();
                }
            }
            reader?.Dispose();

            if (output.Count > 0 && !cancellationToken.IsCancellationRequested)
            {
                batchCount++;
                total += output.Count;
                _logger.LogInformation($"sending batch #{batchCount}, count: {total} records");
                await onBatchReceived(output);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("kusto query is cancelled");
            }

            _logger.LogInformation($"total of {output.Count} records retrieved from kusto");
        }

        public void Dispose()
        {
            _adminClient?.Dispose();
            _queryClient?.Dispose();
        }

        /// <summary>
        /// ObjectReader&ltT&gt is buggy and only relies FieldInfo then passing nameBasedColumnMapping=true
        /// https://msazure.visualstudio.com/_search?action=contents&text=ObjectReader&type=code&lp=custom-Collection&filters=&pageSize=25&result=DefaultCollection%2FOne%2FAzure-Kusto-Service%2FGBdev%2F%2FSrc%2FCommon%2FKusto.Cloud.Platform%2FData%2FTypedDataReader.cs
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        private Dictionary<int, (PropertyInfo prop, Func<object, object> converter)> BuildFieldMapping<T>(IDataReader reader)
        {
            var type = typeof(T);
            var constructor = type.GetConstructors().SingleOrDefault(c => c.GetParameters().Count() == 0);
            if (constructor == null)
            {
                throw new Exception($"type {type.Name} doesn't have parameterless constructor");
            }

            var propMappings = new Dictionary<int, (PropertyInfo prop, Func<object, object> converter)>();
            var fieldTable = reader.GetSchemaTable();
            foreach(DataColumn col in fieldTable.Columns)
            {
                _logger.LogInformation(col.ColumnName);
            }
            for (var i = 0; i < fieldTable.Rows.Count; i++)
            {
                var fieldName = (string)fieldTable.Rows[i]["ColumnName"];
                var property = type.GetProperty(fieldName);
                var dataType = (Type)fieldTable.Rows[i]["DataType"];
                if (property != null)
                {
                    Func<object, object> converter = null;
                    if (!property.PropertyType.IsAssignableFrom(dataType))
                    {
                        converter = CreateConverter(dataType, property.PropertyType);
                    }
                    propMappings.Add(i, (property, converter));
                }
            }
            return propMappings;
        }

        private T Create<T>(IDataReader reader, Dictionary<int, (PropertyInfo prop, Func<object, object> converter)> propMappings)
        {
            T instance = Activator.CreateInstance<T>();
            foreach (var idx in propMappings.Keys)
            {
                var value = reader.GetValue(idx);
                if (value == null || value == DBNull.Value) continue;

                var prop = propMappings[idx].prop;
                if (prop.PropertyType != value.GetType())
                {
                    var converter = propMappings[idx].converter;
                    if (converter != null) {
                        value = converter(value);
                        prop.SetValue(instance, value);
                    }
                    else
                    {
                        try
                        {
                            if (Nullable.GetUnderlyingType(prop.PropertyType) != null)
                            {
                                var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType);
                                value = Convert.ChangeType(value.ToString(), underlyingType);
                            }
                            else
                            {
                                value = Convert.ChangeType(value.ToString(), prop.PropertyType);
                            }
                            prop.SetValue(instance, value);
                        }
                        catch
                        {
                            _logger.LogWarning($"Faile to convert type for column: {prop.Name}, value: {value}");
                        }
                    }
                }
                else 
                {
                    prop.SetValue(instance, value);
                }
            }
            return instance;
        }

        private Func<object, object> CreateConverter(Type srcType, Type tgtType)
        {
            if (tgtType.IsEnum && srcType == typeof(string))
            {
                Func<object, object> converter = s => Enum.Parse(tgtType, (string)s, true);
                return converter;
            }
            if (tgtType == typeof(bool) && srcType == typeof(SByte))
            {
                Func<object, object> converter = s => Convert.ChangeType(s, tgtType);
                return converter;
            }

            return null;
        }
    }
}
