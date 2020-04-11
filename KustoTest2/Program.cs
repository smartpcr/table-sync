using KustoTest2.Aad;
using KustoTest2.Config;
using KustoTest2.DocDb;
using KustoTest2.Instrumentation;
using KustoTest2.Kusto;
using KustoTest2.KV;
using KustoTest2.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace KustoTest2
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ThreadPool.SetMinThreads(100, 100);
            ServicePointManager.DefaultConnectionLimit = 50;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var builder = new HostBuilder()
                .ConfigureServices((hostingContext, services) =>
                {
                    ConfigureServices(services);
                })
                .UseConsoleLifetime();

            using (var host = builder.Build())
            {
                //using (var app = host.Services.GetRequiredService<SyncKustoTableWorker>())
                //{
                //    await app.ExecuteAsync(new CancellationToken());
                //}

                using (var app = host.Services.GetRequiredService<PopulateDeviceAssociations>())
                {
                    await app.ExecuteAsync(new CancellationToken());
                }
            }
            //await builder.RunConsoleAsync();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT")}.json", optional: true, reloadOnChange: false)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();
            services.AddSingleton<IConfiguration>(config);
            Console.WriteLine("registered configuration");

            // options
            services
                .ConfigureSettings<AppInsightsSettings>()
                .ConfigureSettings<AadSettings>()
                .ConfigureSettings<VaultSettings>()
                .ConfigureSettings<BlobStorageSettings>()
                .ConfigureSettings<DocDbSettings>()
                .ConfigureSettings<KustoSettings>()
                .ConfigureSettings<DocDbData>()
                .AddOptions();

            // kv client
            services.AddKeyVault(config);

            // app insights metrics and logging
            services.AddAppInsights(config);

            // contract implementation
            services.AddSingleton<IBlobClient, OldBlobClient>();
            services.AddSingleton<IDocDbClient, DocDbClient>();
            services.AddSingleton<IKustoClient, KustoClient>();
            services.TryAddSingleton<SyncKustoTableWorker>();
            services.TryAddSingleton<PopulateDeviceAssociations>();

            services.AddHostedService<PingBlobWorker>();
        }
    }
}
