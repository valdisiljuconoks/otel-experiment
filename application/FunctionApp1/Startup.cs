using System;
using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using FunctionApp1;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

[assembly: FunctionsStartup(typeof(Startup))]

namespace FunctionApp1;

internal class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddOpenTelemetryTracing(b =>
        {
            b.AddHttpClientInstrumentation();
            b.AddSource("AzureFunctionsOpenTelemetry");
            b.AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));
        });

        builder.AddOpenTelemetry(b =>
        {
            b.IncludeFormattedMessage = true;
            b.IncludeScopes = true;
            b.ParseStateValues = true;
            b.AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));
        });

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IExtensionConfigProvider, ServiceBusExtensionExtensionsConfigProvider>());

        builder.Services.AddTransient(_ => new ActivitySource("AzureFunctionsOpenTelemetry"));
    }
}

[Extension("ServiceBusExtensions")]
internal class ServiceBusExtensionExtensionsConfigProvider : IExtensionConfigProvider
{
    public void Initialize(ExtensionConfigContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        //context.AddConverter(new MessageToDiagnosticsConverter());
    }
}

//public class MessageToDiagnosticsConverter : IConverter<ServiceBusReceivedMessage, string>
//{

//}

//public class Class111 : ServiceBusReceivedMessage
//{

//}

public static class OpenTelemetryLoggingExtensions
{
    public static IFunctionsHostBuilder AddOpenTelemetry(
        this IFunctionsHostBuilder builder,
        Action<OpenTelemetryLoggerOptions> configure = null)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, OpenTelemetryLoggerProvider>());

        if (configure != null)
        {
            builder.Services.Configure(configure);
        }

        return builder;
    }
}
