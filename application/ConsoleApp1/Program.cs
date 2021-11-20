using System.Diagnostics;
using Microsoft.Data.SqlClient;
using OpenTelemetry.Trace;
using OpenTelemetry;

// This is required if the collector doesn't expose an https endpoint
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetSampler(new AlwaysOnSampler())
    .AddSource("MyApplicationBackgroundWorkerActivitySource")
    .AddSqlClientInstrumentation(opt =>
    {
        opt.SetDbStatementForText = true;
        opt.EnableConnectionLevelAttributes = true;
        opt.RecordException = true;
    })
    .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"))
    .Build();

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

var activitySource = new ActivitySource("MyApplicationBackgroundWorkerActivitySource");

using var activity = activitySource.StartActivity("Working in background", ActivityKind.Server,
    new ActivityContext(
        ActivityTraceId.CreateFromString("384ff52518295958fdd14ad8f635bd51"),
        ActivitySpanId.CreateRandom(),
        ActivityTraceFlags.None));

activity?.AddBaggage("product.id", "12345");

using var computeActivity = activitySource.StartActivity("calculating smth");
await Task.Delay(200);
computeActivity?.Stop();

using var queryActivity = activitySource.StartActivity("query for data");
using var conn = new SqlConnection("Server=.;Initial Catalog=user1db;Persist Security Info=False;User ID=user1;Password=password;MultipleActiveResultSets=False;Connection Timeout=30;");
conn.Open();

var cmd = new SqlCommand("SELECT * FROM Table1", conn);
cmd.ExecuteReader();

conn.Close();