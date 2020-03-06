using EnsureThat;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KustoTest2.Instrumentation
{
    /// <summary>
    /// A wrapper to app insights method TelemetryClient.StartOperation that will create
    /// either RequestTelemetry or DependencyTelemetry based on if current call is root
    /// This class is returned in using statement in order to emit metric when it's being disposed
    /// </summary>
    public sealed class OperationScope : IDisposable
    {
        public const string RequestTelemetryKey = "request-id";

        private readonly TelemetryClient telemetry;

        private IOperationHolder<RequestTelemetry> requestOperation;

        public OperationScope(Activity activity, TelemetryClient telemetryClient = null)
        {
            Ensure.That(activity).IsNotNull();

            telemetry = telemetryClient;
            if (telemetry == null)
            {
                return;
            }

            requestOperation = telemetry.StartOperation<RequestTelemetry>(activity);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (requestOperation != null)
                {
                    telemetry?.StopOperation(requestOperation);

                    requestOperation.Dispose();
                }
            }

            requestOperation = null;
        }

        /// <summary>
        /// A helper to create <see cref="IDisposable"/> from caller class
        /// Example:
        /// using (_metrics.StartOperation("methodName")) {}
        /// using (_metrics.StartOperation("methodName", parentOperationId)) {}
        ///
        /// if parentId is not passed in, it checks static instance AsyncLocal to find current activity,
        /// which is stored as local copy for each call (ExecutionContext) within a process.
        /// When parentId is not found, it assumes current call is the root
        /// </summary>
        public static IDisposable StartOperation(
            string parentOperationId,
            string operationName,
            TelemetryClient appInsights)
        {
            var activity = new Activity(operationName);

            if (!string.IsNullOrEmpty(parentOperationId))
            {
                activity.SetParentId(parentOperationId);
            }
            else
            {
                activity.SetIdFormat(ActivityIdFormat.W3C);
            }

            return new OperationScope(activity, appInsights);
        }
    }
}
