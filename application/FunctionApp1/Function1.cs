using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using FunctionApp1.Infrastructure;
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

        log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
    }
}
