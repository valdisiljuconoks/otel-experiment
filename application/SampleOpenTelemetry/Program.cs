using System.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;

// This is required if the collector doesn't expose an https endpoint
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenTelemetryMetrics(builder =>
{
    builder.AddHttpClientInstrumentation();
    builder.AddAspNetCoreInstrumentation();
    builder.AddMeter("MyApplicationMetrics");
    builder.AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));
});

builder.Services.AddOpenTelemetryTracing(builder =>
{
    builder.AddAspNetCoreInstrumentation();
    builder.AddHttpClientInstrumentation();
    builder.AddSource("MyApplicationActivitySource");
    builder.AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));
});

builder.Logging.AddOpenTelemetry(builder =>
{
    builder.IncludeFormattedMessage = true;
    builder.IncludeScopes = true;
    builder.ParseStateValues = true;
    builder.AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));
});

builder.Services.AddRazorPages();

var app = builder.Build();

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

app.UseRouting();
app.MapRazorPages();

app.Run();