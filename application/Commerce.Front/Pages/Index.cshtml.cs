using System.Diagnostics;
using System.Diagnostics.Metrics;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Commerce.Front.Pages;

public class IndexModel : PageModel
{
    private readonly ActivitySource _activitySource;
    private readonly HttpClient _httpClient;
    private readonly ILogger<IndexModel> _logger;
    private readonly ServiceBusSender _sender;

    public IndexModel(ILogger<IndexModel> logger, IConfiguration config)
    {
        _logger = logger;
        _activitySource = new ActivitySource("Commerce.Front");
        _httpClient = new HttpClient();
        var client = new ServiceBusClient(config.GetConnectionString("ServiceBusConnectionString"));
        _sender = client.CreateSender("otel-sameple-queue");
    }

    [BindProperty] public string? TraceId { get; set; }
    [BindProperty] public string? ActionId { get; set; }
    [BindProperty] public string? ProductId { get; set; }

    public IActionResult OnGet()
    {
        ActionId = Activity.Current?.Id;
        TraceId = Activity.Current?.TraceId.ToString();

        return Page();
    }


    public async Task<IActionResult> OnPost()
    {
        if (string.IsNullOrEmpty(ActionId))
        {
            throw new InvalidOperationException("Argument " + nameof(ActionId) + " is null");
        }

        using (var activity = _activitySource.StartActivity("Handling checkout request...", ActivityKind.Server, ActionId))
        {
            activity?.AddBaggage("product.id", ProductId);

            _logger.LogInformation("Sending request out to API endpoint to checkout");

            await _httpClient.GetStringAsync("https://localhost:7259/checkout");

            // emitting message on the queue
            var serviceBusMessage = new ServiceBusMessage("checkout");
            foreach (var baggage in Activity.Current?.Baggage ?? Array.Empty<KeyValuePair<string, string?>>())
            {
                serviceBusMessage.ApplicationProperties.Add(baggage.Key, baggage.Value);
            }

            await _sender.SendMessageAsync(serviceBusMessage);
        }

        return Page();
    }
}
