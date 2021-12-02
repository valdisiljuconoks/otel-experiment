using System;
using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace FunctionApp1;

public class Function1
{
    [FunctionName("Function1")]
    public void Run([ServiceBusTrigger("otel-sameple-queue", Connection = "ServiceBusConnectionString")] ServiceBusReceivedMessage myQueueItem,
        ILogger log)
    {
        var activitySource = new ActivitySource("AzureFunctionsOpenTelemetry");

        var diagnosticContextId = myQueueItem.ApplicationProperties["Diagnostic-Id"].ToString() ?? throw new NullReferenceException("Missing Diagnostic-Id property on the message.");

        using var activity = activitySource.StartActivity(
            "Handling SB message in AzFunc...",
            ActivityKind.Server,
            diagnosticContextId);

        activity?.AddBaggage("product.id", "12345");

        // call again /compute endpoint

        log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
    }
}
