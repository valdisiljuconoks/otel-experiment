using System.Diagnostics;
using System.Diagnostics.Metrics;
using Commerce.Api;
using Commerce.Common;

// This is required if the collector doesn't expose an https endpoint
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("Commerce.Api", "Commerce.Api.Metrics", new Uri("http://localhost:4320"));
builder.AddActivityBaggagePropagation();

builder.Services.AddTransient(_ => new ActivitySource("Commerce.Api"));
builder.Services.AddSingleton(_ => new Meter("Commerce.Api.Metrics"));
builder.Services.AddCors(options =>
{
    options.AddPolicy("_FrontendCors",
        b =>
        {
            b.WithOrigins("https://localhost:7258");
            b.WithHeaders("traceparent");
        });
});

var app = builder.Build();

app.UseCors();
app.MapGet("/", () => Results.Ok("Awaiting for requests..."));

app.Map("/checkout", async (HttpContext context, ActivitySource activitySource, ILogger<CheckoutProcess> logger) =>
{
    using (var _ = activitySource.StartActivity("Checkout sales order"))
    {
        logger.LogInformation("Starting to process checkout...");
        await Task.Delay(500);

        logger.LogInformation("Checkout processed.");
    }

    return Results.Ok();
}).RequireCors("_FrontendCors");


app.Map("/get-availability", async (HttpContext context, ActivitySource activitySource, Meter meter) =>
{
    using (var _ = activitySource.StartActivity("Querying database for availability..."))
    {
        var counter = meter.GetCounter<int>("get_availability_requests");
        counter?.Add(1);

        var histogram = meter.CreateHistogram<float>("get_availability_duration", "ms");

        var duration = Random.Shared.Next(1000, 1500);

        await Task.Delay(duration);

        histogram.Record(duration, KeyValuePair.Create<string, object?>("Product.Id", "54321"));
    }

    return Results.Ok();
}).RequireCors("_FrontendCors");

app.Run();

public class CheckoutProcess { }
