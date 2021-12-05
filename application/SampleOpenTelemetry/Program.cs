using System.Diagnostics;
using Microsoft.Extensions.FileProviders;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

// This is required if the collector doesn't expose an https endpoint
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetryMetrics(b =>
{
    b.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("SampleOpenTelemetry"));
    b.AddHttpClientInstrumentation();
    b.AddAspNetCoreInstrumentation();
    b.AddMeter("SampleOpenTelemetryMetrics");
    b.AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));
});

builder.Services.AddOpenTelemetryTracing(b =>
{
    b.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("SampleOpenTelemetry"));
    b.AddAspNetCoreInstrumentation();
    b.AddHttpClientInstrumentation();
    b.AddSource("SampleOpenTelemetry");
    b.AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));
});

builder.Logging.AddOpenTelemetry(b =>
{
    b.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("SampleOpenTelemetry"));
    b.IncludeFormattedMessage = true;
    b.IncludeScopes = true;
    b.ParseStateValues = true;
    b.AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));
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

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "dist")),
    RequestPath = "/dist"
});
app.UseRouting();
app.MapRazorPages();

app.Run();