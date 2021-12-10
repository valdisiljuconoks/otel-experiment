using System.Diagnostics;
using Azure.Messaging.ServiceBus;

namespace FunctionApp1.Infrastructure;

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