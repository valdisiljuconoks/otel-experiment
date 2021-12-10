using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpenTelemetry.Common;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddObservability(
        this WebApplicationBuilder builder,
        string serviceName,
        string meterName,
        Uri collectorUri)
    {
        builder.Services.AddOpenTelemetryMetrics(b =>
        {
            b.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
            b.AddHttpClientInstrumentation();
            b.AddAspNetCoreInstrumentation();
            b.AddMeter(meterName);
            b.AddOtlpExporter(options => options.Endpoint = collectorUri);
        });

        builder.Services.AddOpenTelemetryTracing(b =>
        {
            b.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
            b.AddAspNetCoreInstrumentation();
            b.AddHttpClientInstrumentation();
            b.AddSource(serviceName);
            b.AddOtlpExporter(options => options.Endpoint = collectorUri);
        });

        builder.Logging.AddOpenTelemetry(b =>
        {
            b.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
            b.IncludeFormattedMessage = true;
            b.IncludeScopes = true;
            b.ParseStateValues = true;
            b.AddOtlpExporter(options => options.Endpoint = collectorUri);
        });

        return builder;
    }

    public static WebApplicationBuilder AddActivityBaggagePropagation(this WebApplicationBuilder builder)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            ActivityStopped = activity =>
            {
                foreach (var (key, value) in activity.Baggage)
                {
                    activity.AddTag(key, value);
                }
            }
        };

        ActivitySource.AddActivityListener(listener);

        return builder;
    }
}
