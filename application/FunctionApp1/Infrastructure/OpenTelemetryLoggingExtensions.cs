using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;

namespace FunctionApp1.Infrastructure;

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