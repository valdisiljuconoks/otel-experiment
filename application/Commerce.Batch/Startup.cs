using System;
using System.Diagnostics;
using Commerce.Batch;
using Commerce.Batch.Infrastructure;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Commerce.Batch;

internal class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.AddObservability("Commerce.Batch", new Uri("http://localhost:4317"));
        builder.AddActivityBaggagePropagation();

        builder.Services.AddTransient(_ => new ActivitySource("Commerce.Batch"));
    }
}
