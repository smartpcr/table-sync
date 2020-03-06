using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using KustoTest2.Aad;
using KustoTest2.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KustoTest2.Kusto
{
    public class ClientFactory
    {
        private readonly ILogger<ClientFactory> _logger;
        private readonly ICslQueryProvider _client;
        private readonly ICslAdminProvider _adminClient;

        public ClientFactory(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ClientFactory>();
            var aadSettings = configuration.GetConfiguredSettings<AadSettings>();
            var kustoSettings = configuration.GetConfiguredSettings<KustoSettings>();
            var authBuilder = new AadAuthBuilder(aadSettings);
            var clientSecretCert = authBuilder.GetClientSecretOrCert();
            KustoConnectionStringBuilder kcsb;
            if (kustoSettings.AuthMode == AuthMode.User)
            {
                kcsb = new KustoConnectionStringBuilder(kustoSettings.ClusterUrl, kustoSettings.DbName)
                {
                    FederatedSecurity = true,
                    Authority = aadSettings.Authority
                }.WithAadUserPromptAuthentication();
            }
            else if (clientSecretCert.secret != null)
            {
                kcsb = new KustoConnectionStringBuilder($"{kustoSettings.ClusterUrl}")
                    .WithAadApplicationKeyAuthentication(
                        aadSettings.ClientId,
                        clientSecretCert.secret,
                        aadSettings.Authority);
            }
            else
            {
                kcsb = new KustoConnectionStringBuilder($"{kustoSettings.ClusterUrl}")
                    .WithAadApplicationCertificateAuthentication(
                        aadSettings.ClientId,
                        clientSecretCert.cert,
                        aadSettings.Authority);
            }
            _client = KustoClientFactory.CreateCslQueryProvider(kcsb);
            _adminClient = KustoClientFactory.CreateCslAdminProvider(kcsb);
        }

        public ICslQueryProvider QueryClient => _client;
        public ICslAdminProvider AdminClient => _adminClient;
    }
}
