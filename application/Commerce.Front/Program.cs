using Commerce.Common;
using Microsoft.Extensions.FileProviders;

// This is required if the collector doesn't expose an https endpoint
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("Commerce.Front", "SampleOpenTelemetryMetrics", new Uri("http://localhost:4320"));
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
