using KustoTest2.Aad;
using KustoTest2.Config;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace KustoTest2.KV
{
    public static class KeyVaultBuilder
    {
        public static IServiceCollection AddKeyVault(this IServiceCollection services, IConfiguration configuration)
        {
            var aadSettings = configuration.GetConfiguredSettings<AadSettings>();
            var authBuilder = new AadAuthBuilder(aadSettings);

            Task<string> AuthCallback(string authority, string resource, string scope) => authBuilder.GetAccessTokenAsync(resource);
            var kvClient = new KeyVaultClient(AuthCallback);
            services.AddSingleton<IKeyVaultClient>(kvClient);

            return services;
        }
    }
}
