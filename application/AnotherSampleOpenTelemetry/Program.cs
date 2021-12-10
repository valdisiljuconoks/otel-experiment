using System.Diagnostics;
using OpenTelemetry.Common;

// This is required if the collector doesn't expose an https endpoint
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("AnotherSampleOpenTelemetry", "MyApplicationMetrics", new Uri("http://localhost:4317"));
builder.AddActivityBaggagePropagation();

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
