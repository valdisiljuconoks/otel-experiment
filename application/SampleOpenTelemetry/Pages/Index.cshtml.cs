using System.Diagnostics;
using System.Diagnostics.Metrics;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SampleOpenTelemetry.Pages;

public class IndexModel : PageModel
{
    private readonly ActivitySource _activitySource;
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

        var client = new ServiceBusClient(config.GetConnectionString("ServiceBusConnectionString"));
        _sender = client.CreateSender("otel-sameple-queue");
    }

    [BindProperty] public string? ActionId { get; set; }
    [BindProperty] public string ProductId { get; set; }

    public IActionResult OnGet()
    {
        _activitySource.StartActivity("On GET.");

        ActionId = Activity.Current?.Id;

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        _requestCounter.Add(1);

        if (string.IsNullOrEmpty(ActionId))
        {
            throw new InvalidOperationException("Argument " + nameof(ActionId) + " is null");
        }

        using (var activity = _activitySource.StartActivity("Get data", ActivityKind.Server, ActionId))
        {
            activity?.AddBaggage("product.id", ProductId);

            var str1 = await _httpClient.GetStringAsync("https://www.google.com");

            await _httpClient.GetStringAsync("https://localhost:7259/compute");

            _logger.LogInformation("Response1 length: {Length}", str1.Length);

            // emitting message on the queue
            var serviceBusMessage = new ServiceBusMessage("testing");

            foreach (var baggage in Activity.Current?.Baggage ?? Array.Empty<KeyValuePair<string, string?>>())
            {
                serviceBusMessage.ApplicationProperties.Add(baggage.Key, baggage.Value);
            }

            await _sender.SendMessageAsync(serviceBusMessage);
        }

        return Page();
    }
}
