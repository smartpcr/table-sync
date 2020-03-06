﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KustoTest2.Aad
{
    public class AadSettings
    {
        public const string MicrosoftAadLoginUrl = "https://login.microsoftonline.com/";
        public const string MicrosoftAadTenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        // TODO: add GME, AME tenant

        /// <summary>
        /// Gets the AAD tenant login URL. See also <see cref="MicrosoftAadLoginUrl"/>
        /// </summary>
        public string Instance { get; set; }

        /// <summary>
        /// Gets the AAD tenant. See also <see cref="MicrosoftAadTenantId"/>
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets the ClientId. Note that ClientId is obsolete name for ApplicationId.
        /// </summary>
        public string ClientId { get; set; }

        public string[] Scopes { get; set; }

        public string Authority => $"{Instance}{TenantId}";

        public static AadSettings ForMicrosoftTenant(string appId) => new AadSettings
        {
            Instance = MicrosoftAadLoginUrl,
            TenantId = MicrosoftAadTenantId,
            ClientId = appId
        };

        public string ClientSecretFile { get; set; }
        public string ClientCertFile { get; set; }
    }
}
