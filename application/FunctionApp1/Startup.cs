using System;
using FunctionApp1;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
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
    }
}

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

