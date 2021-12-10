using System;
using System.Diagnostics;
using FunctionApp1;
using FunctionApp1.Infrastructure;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace FunctionApp1;

internal class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.AddObservability("FunctionApp1", new Uri("http://localhost:4317"));
        builder.AddActivityBaggagePropagation();

        builder.Services.AddTransient(_ => new ActivitySource("AzureFunctionsOpenTelemetry"));
    }
}
