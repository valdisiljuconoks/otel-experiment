using System.Diagnostics;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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
        foreach (var (key, value) in activity.Baggage) activity.AddTag(key, value);
    }
};
ActivitySource.AddActivityListener(listener);
var activitySource = new ActivitySource("AnotherSampleOpenTelemetry");

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

app.Map("/compute", async (HttpContext context) =>
{
    using (var _ = activitySource.StartActivity("Compute data", ActivityKind.Server)) { await Task.Delay(500); }

    return Results.Ok();
}).RequireCors("_FrontendCors");

app.Map("/compute-for-frontend", async (HttpContext context) =>
{
    using (var _ = activitySource.StartActivity("Frontend compute data", ActivityKind.Server)) { await Task.Delay(1200); }

    return Results.Ok();
}).RequireCors("_FrontendCors");

app.Run();
