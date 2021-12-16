using System.Diagnostics;
using System.Diagnostics.Metrics;
using Commerce.Common;

// This is required if the collector doesn't expose an https endpoint
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("Commerce.Api", "Commerce.Api.Metrics", new Uri("http://localhost:4317"));
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

app.Map("/checkout", async (HttpContext context, ActivitySource activitySource) =>
{
    using (var _ = activitySource.StartActivity("Checkout sales order"))
    {
        await Task.Delay(500);
    }

    return Results.Ok();
}).RequireCors("_FrontendCors");


app.Map("/get-availability", async (HttpContext context, ActivitySource activitySource, Meter meter) =>
{
    using (var _ = activitySource.StartActivity("Querying database for availability..."))
    {
        var counter = meter.CreateCounter<int>("get_availability_requests");
        counter.Add(1);

        await Task.Delay(1200);
    }

    return Results.Ok();
}).RequireCors("_FrontendCors");

app.Run();
