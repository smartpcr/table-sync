using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KustoTest2.Storage
{
    public interface IBlobClient
    {
        Task UploadAsync(string blobFolder, string blobName, string blobContent, CancellationToken cancellationToken);

        Task UploadBatchAsync<T>(string blobFolder, Func<T, string> getName, IList<T> list, CancellationToken cancellationToken);

        Task DownloadAsync(string blobFolder, string blobName, string localFolder, CancellationToken cancellationToken);

        Task<List<T>> ListAsync<T>(string blobFolder, Func<string, bool> filter, CancellationToken cancellationToken);

        Task<int> CountAsync<T>(string blobFolder, Func<T, bool> filter, CancellationToken cancellationToken);

        Task<IList<T>> TryAcquireLease<T>(string blobFolder, int take, Action<T> update, TimeSpan timeout);

        Task ReleaseLease(string blobName);

        Task DeleteBlobs(string blobFolder, CancellationToken cancellationToken);
    }
}
