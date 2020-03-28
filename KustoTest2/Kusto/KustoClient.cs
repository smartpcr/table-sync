using Kusto.Cloud.Platform.Data;
using Kusto.Data.Common;
using KustoTest2.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
            await Read(reader, onBatchReceived, cancellationToken, batchSize);
        }

        public async Task ExecuteQuery(
            Type entityType,
            string query,
            Func<IList<object>, Task> onBatchReceived,
            CancellationToken cancellationToken = default,
            int batchSize = 1000)
        {
            var reader = await _queryClient.ExecuteQueryAsync(
                _kustoSettings.DbName,
                query,
                new ClientRequestProperties() { ClientRequestId = Guid.NewGuid().ToString() });
            await Read(entityType, reader, onBatchReceived, cancellationToken, batchSize);
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
            await Read(reader, onBatchReceived, cancellationToken, batchSize);
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

        private async Task Read(Type entityType, IDataReader reader, Func<IList<object>, Task> onBatchReceived, CancellationToken cancellationToken, int batchSize)
        {
            var propMappings = BuildFieldMapping(entityType, reader);

            var output = new List<object>();
            int batchCount = 0;
            int total = 0;
            while (reader.Read() && !cancellationToken.IsCancellationRequested)
            {
                var instance = Create(entityType, reader, propMappings);
                output.Add(instance);
                if (output.Count >= batchSize)
                {
                    batchCount++;
                    total += output.Count;
                    await onBatchReceived(output);
                    _logger.LogInformation($"sending batch #{batchCount}, total: {total} records");
                    output = new List<object>();
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
        /// ObjectReader is buggy and only relies FieldInfo then passing nameBasedColumnMapping=true
        /// https://msazure.visualstudio.com/_search?action=contents&text=ObjectReader&type=code&lp=custom-Collection&filters=&pageSize=25&result=DefaultCollection%2FOne%2FAzure-Kusto-Service%2FGBdev%2F%2FSrc%2FCommon%2FKusto.Cloud.Platform%2FData%2FTypedDataReader.cs
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        private Dictionary<int, (PropertyInfo prop, Func<object, object> converter)> BuildFieldMapping<T>(IDataReader reader)
        {
            return BuildFieldMapping(typeof(T), reader);
        }

        private Dictionary<int, (PropertyInfo prop, Func<object, object> converter)> BuildFieldMapping(Type type, IDataReader reader)
        {
            var constructor = type.GetConstructors().SingleOrDefault(c => !c.GetParameters().Any());
            if (constructor == null)
            {
                throw new Exception($"type {type.Name} doesn't have parameterless constructor");
            }

            // handle json property mappings
            var props = type.GetProperties().Where(p => p.CanWrite).ToList();
            var propNameMappings = new Dictionary<string, PropertyInfo>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var prop in props)
            {
                var jsonProp = prop.GetCustomAttribute<JsonPropertyAttribute>();
                if (jsonProp != null)
                {
                    propNameMappings.Add(jsonProp.PropertyName, prop);
                }
                else
                {
                    propNameMappings.Add(prop.Name, prop);
                }
            }

            var propMappings = new Dictionary<int, (PropertyInfo prop, Func<object, object> converter)>();
            var fieldTable = reader.GetSchemaTable();
            if (fieldTable == null)
            {
                throw new InvalidOperationException("Query doesn't return schema info");
            }

            for (var i = 0; i < fieldTable.Rows.Count; i++)
            {
                var fieldName = (string)fieldTable.Rows[i]["ColumnName"];
                var property = type.GetProperty(fieldName);
                if (property == null)
                {
                    propNameMappings.TryGetValue(fieldName, out property);
                }
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
                else
                {
                    _logger.LogWarning($"Missing mapping for field: {fieldName}");
                }
            }
            return propMappings;
        }

        private T Create<T>(IDataReader reader, Dictionary<int, (PropertyInfo prop, Func<object, object> converter)> propMappings)
        {
            return (T)Create(typeof(T), reader, propMappings);
        }

        private object Create(Type type, IDataReader reader, Dictionary<int, (PropertyInfo prop, Func<object, object> converter)> propMappings)
        {
            var instance = Activator.CreateInstance(type);
            foreach (var idx in propMappings.Keys)
            {
                var value = reader.GetValue(idx);
                if (value == null || value == DBNull.Value) continue;

                var prop = propMappings[idx].prop;
                if (prop.PropertyType != value.GetType())
                {
                    var converter = propMappings[idx].converter;
                    if (converter != null)
                    {
                        value = converter(value);
                        prop.SetValue(instance, value);
                    }
                    else
                    {
                        try
                        {
                            var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType);
                            value = Convert.ChangeType(
                                value.ToString(), 
                                underlyingType != null ? underlyingType : prop.PropertyType);
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
                object Converter(object s) => Enum.Parse(tgtType, (string) s, true);
                return Converter;
            }
            if (tgtType == typeof(bool) && srcType == typeof(SByte))
            {
                object Converter(object s) => Convert.ChangeType(s, tgtType);
                return Converter;
            }
            if (tgtType == typeof(string[]))
            {
                object Converter(object s)
                {
                    var stringValue = s.ToString().Trim().Trim(new[] {'[', ']'});
                    var items = stringValue.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(a => a.Trim().Trim(new[] {'"'}).Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToArray();
                    return items;
                }

                return Converter;
            }

            return null;
        }
    }
}
