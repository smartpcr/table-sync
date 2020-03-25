using System;
using System.Threading;
using System.Threading.Tasks;
using KustoTest2.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace KustoTest2
{
    public class PingBlobWorker : BackgroundService
    {
        private readonly ILogger<PingBlobWorker> logger;
        private readonly IBlobClient blobClient;

        public PingBlobWorker(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<PingBlobWorker>();
            blobClient = serviceProvider.GetRequiredService<IBlobClient>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var random = new Random(DateTime.Now.Second);
            while (!stoppingToken.IsCancellationRequested)
            {
                var payload = new
                {
                    id = Guid.NewGuid(),
                    timestamp = DateTime.Now,
                    value = random.Next(1000)
                };
                var blobName = $"{payload.id}.json";
                var blobFolder = DateTime.Now.ToString("yyyy/MM/dd/HH/mm");
                await blobClient.UploadAsync(blobFolder, blobName, JsonConvert.SerializeObject(payload), stoppingToken);

                await Task.Delay(TimeSpan.FromMinutes(30));
            }
        }
    }
}
