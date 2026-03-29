using System.Text;
using System.Text.Json;
using RestAPI.Constantas;

namespace RestAPI.Services.Webhook;

public class WebhookService : IWebhookService
{
    private readonly HttpClient _httpClient;
    private readonly string _webhookUrl;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(HttpClient httpClient, IConfiguration config, ILogger<WebhookService> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _webhookUrl = config["Webhook:Url"]!;
        _logger = logger;
    }

    public async Task SendAsync(string eventName, object data)
    {
        var payload = new WebhookPayload
        {
            Event = eventName,
            Timestamp = DateTime.UtcNow,
            Data = data
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(_webhookUrl, content);
            _logger.LogInformation("Webhook [{Event}] sent. Status: {Status}", eventName, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook [{Event}] failed to send.", eventName);
        }
    }
}
