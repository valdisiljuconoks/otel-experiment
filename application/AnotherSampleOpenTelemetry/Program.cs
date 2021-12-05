using System.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

// This is required if the collector doesn't expose an https endpoint
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenTelemetryMetrics(b =>
{
    b.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("AnotherSampleOpenTelemetry"));
    b.AddHttpClientInstrumentation();
    b.AddAspNetCoreInstrumentation();
    b.AddMeter("MyApplicationMetrics");
    b.AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));
});

builder.Services.AddOpenTelemetryTracing(b =>
{
    b.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("AnotherSampleOpenTelemetry"));
    b.AddAspNetCoreInstrumentation();
    b.AddHttpClientInstrumentation();
    b.AddSource("AnotherSampleOpenTelemetry");
    b.AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));
});

builder.Logging.AddOpenTelemetry(b =>
{
    b.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("AnotherSampleOpenTelemetry"));
    b.IncludeFormattedMessage = true;
    b.IncludeScopes = true;
    b.ParseStateValues = true;
    b.AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));
});

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

var app = builder.Build();

var activitySource = new ActivitySource("AnotherSampleOpenTelemetry");

app.MapGet("/", () => Results.Ok("Awaiting for requests..."));

app.MapGet("/compute", async () =>
{
    using (var _ = activitySource.StartActivity("Compute data", ActivityKind.Server))
    {
        await Task.Delay(500);
    }

    return Results.Ok();
});

app.Run();