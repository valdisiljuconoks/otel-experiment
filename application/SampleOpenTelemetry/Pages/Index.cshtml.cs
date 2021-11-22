using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SampleOpenTelemetry.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly Counter<int> _requestCounter;
        private readonly ActivitySource _activitySource;
        private readonly HttpClient _httpClient;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
            _activitySource = new ActivitySource("MyApplicationActivitySource");

            var meter = new Meter("MyApplicationMetrics");
            _requestCounter = meter.CreateCounter<int>("compute_requests");
            _httpClient = new HttpClient();
        }

        public string? TraceId { get; set; }

        public async Task<IActionResult> OnGet()
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
            }

            return Page();
        }
    }
}
