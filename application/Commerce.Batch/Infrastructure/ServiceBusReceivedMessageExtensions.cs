using System;
using Azure.Messaging.ServiceBus;

namespace Commerce.Batch.Infrastructure;

public static class ServiceBusReceivedMessageExtensions
{
    public static string GetContextId(this ServiceBusReceivedMessage message)
    {
        var z =
            message.ApplicationProperties["Diagnostic-Id"].ToString()
            ?? throw new NullReferenceException("Missing Diagnostic-Id property on the message.");

        return z;
    }
}