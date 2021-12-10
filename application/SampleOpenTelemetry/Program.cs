using Microsoft.Extensions.FileProviders;
using OpenTelemetry.Common;

// This is required if the collector doesn't expose an https endpoint
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("SampleOpenTelemetry", "SampleOpenTelemetryMetrics", new Uri("http://localhost:4317"));
builder.AddActivityBaggagePropagation();

builder.Services.AddRazorPages();

var app = builder.Build();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "dist")),
    RequestPath = "/dist"
});
app.UseRouting();
app.MapRazorPages();

app.Run();
