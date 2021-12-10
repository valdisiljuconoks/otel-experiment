using System;
using System.Diagnostics;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FunctionApp1.Infrastructure;

public static class IFunctionsHostBuilderExtensions
{
    public static IFunctionsHostBuilder AddObservability(this IFunctionsHostBuilder builder, string serviceName, Uri collectorUri)
    {
        builder.Services.AddOpenTelemetryTracing(b =>
        {
            b.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
            b.AddHttpClientInstrumentation();
            b.AddSource("AzureFunctionsOpenTelemetry");
            b.AddOtlpExporter(options => options.Endpoint = collectorUri);
        });

        builder.AddOpenTelemetry(b =>
        {
            b.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
            b.IncludeFormattedMessage = true;
            b.IncludeScopes = true;
            b.ParseStateValues = true;
            b.AddOtlpExporter(options => options.Endpoint = collectorUri);
        });

        return builder;
    }

    public static IFunctionsHostBuilder AddActivityBaggagePropagation(this IFunctionsHostBuilder builder)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            ActivityStopped = activity =>
            {
                foreach (var (key, value) in activity.Baggage) { activity.AddTag(key, value); }
            }
        };

        ActivitySource.AddActivityListener(listener);

        return builder;
    }
}