using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KustoTest2.Kusto
{
    public interface IKustoClient : IDisposable
    {
        Task<IEnumerable<T>> ExecuteQuery<T>(string query);
        Task ExecuteQuery<T>(
            string query, 
            Func<IList<T>, Task> onBatchReceived, 
            CancellationToken cancellationToken = default, 
            int batchSize = 1000);
        Task ExecuteQuery(
            Type entityType,
            string query,
            Func<IList<object>, Task> onBatchReceived,
            CancellationToken cancellationToken = default,
            int batchSize = 1000);
        Task<IEnumerable<T>> ExecuteFunction<T>(string functionName, params (string name, string value)[] parameters);
        Task ExecuteFunction<T>(
            string functionName,
            (string name, string value)[] parameters,
            Func<IList<T>, Task> onBatchReceived,
            CancellationToken cancellationToken = default,
            int batchSize = 1000);
    }
}
