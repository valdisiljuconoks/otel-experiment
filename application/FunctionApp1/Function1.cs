using System;
using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace FunctionApp1;

public class Function1
{
    private readonly ActivitySource _source;

    public Function1(ActivitySource source) { _source = source; }

    [FunctionName("Function1")]
    public void Run(
        [ServiceBusTrigger("otel-sameple-queue", Connection = "ServiceBusConnectionString")]
        ServiceBusReceivedMessage myQueueItem,
        ILogger log)
    {
        using var activity = _source.StartActivity("Handling SB message in AzFunc...", myQueueItem);

        // call again /compute endpoint


        log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
    }
}


public static class ZZ
{
    public static string GetContextId(this ServiceBusReceivedMessage message)
    {
        var z = message.ApplicationProperties["Diagnostic-Id"].ToString()
               ?? throw new NullReferenceException("Missing Diagnostic-Id property on the message.");
        return z;
    }
}

public static class ActivitySourceExtensions
{
    public static Activity StartActivity(this ActivitySource source, string message, ServiceBusReceivedMessage busMessage)
    {
        var diagnosticContextId = busMessage.GetContextId();

        var activity = source.StartActivity(message, ActivityKind.Server, diagnosticContextId);

        foreach (var property in busMessage.ApplicationProperties)
        {
            if (property.Key != "Diagnostic-Id")
            {
                Activity.Current?.AddBaggage(property.Key, property.Value?.ToString());
            }
        }

        return activity;
    }
}
