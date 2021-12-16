using System.Diagnostics;
using OpenTelemetry.Common;

// This is required if the collector doesn't expose an https endpoint
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("AnotherSampleOpenTelemetry", "MyApplicationMetrics", new Uri("http://localhost:4317"));
builder.AddActivityBaggagePropagation();

builder.Services.AddTransient(_ => new ActivitySource("AnotherSampleOpenTelemetry"));
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


app.Map("/get-availability", async (HttpContext context, ActivitySource activitySource) =>
{
    using (var _ = activitySource.StartActivity("Querying database for availability..."))
    {
        await Task.Delay(1200);
    }

    return Results.Ok();
}).RequireCors("_FrontendCors");

app.Run();
