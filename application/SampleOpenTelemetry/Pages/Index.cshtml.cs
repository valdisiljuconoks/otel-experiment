using System.Diagnostics;
using System.Diagnostics.Metrics;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SampleOpenTelemetry.Pages;

public class IndexModel : PageModel
{
    private readonly ActivitySource _activitySource;
    private readonly ServiceBusClient _client;
    private readonly HttpClient _httpClient;
    private readonly ILogger<IndexModel> _logger;
    private readonly Counter<int> _requestCounter;
    private readonly ServiceBusSender _sender;

    public IndexModel(ILogger<IndexModel> logger, IConfiguration config)
    {
        _logger = logger;
        _activitySource = new ActivitySource("SampleOpenTelemetry");

        var meter = new Meter("SampleOpenTelemetryMetrics");
        _requestCounter = meter.CreateCounter<int>("compute_requests");
        _httpClient = new HttpClient();

        _client = new ServiceBusClient(config.GetConnectionString("ServiceBusConnectionString"));
        _sender = _client.CreateSender("otel-sameple-queue");
    }

    public string? TraceId { get; set; }

    [BindProperty] public string CorrelationKey { get; set; }

    public async Task<IActionResult> OnPost()
    {
        _requestCounter.Add(1);

        using (var activity = _activitySource.StartActivity("Get data"))
        {
            activity?.AddBaggage("product.id", "12345");

            TraceId = activity?.TraceId.ToString();

            var str1 = await _httpClient.GetStringAsync("https://example.com");
            var str2 = await _httpClient.GetStringAsync("https://www.google.com");

            await _httpClient.GetStringAsync("https://localhost:7259/compute");

            _logger.LogInformation("Response1 length: {Length}", str1.Length);
            _logger.LogInformation("Response2 length: {Length}", str2.Length);

            // emitting message on the queue
            await _sender.SendMessageAsync(new ServiceBusMessage("testing"));
        }

        return Page();
    }
}
