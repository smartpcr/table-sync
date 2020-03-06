using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using System.Linq;

namespace KustoTest2.Instrumentation
{
    internal class ContextTelemetryInitializer : ITelemetryInitializer
    {
        private AppInsightsSettings _settings;

        public ContextTelemetryInitializer(AppInsightsSettings serviceContext)
        {
            _settings = serviceContext;
        }

        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Cloud.RoleName = _settings.Role;
            telemetry.Context.Component.Version = _settings.Version;
            if (_settings.Tags?.Any() == true)
            {
                telemetry.Context.GlobalProperties["tags"] = string.Join(",", _settings.Tags);
            }
        }
    }
}