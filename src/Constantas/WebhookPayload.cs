namespace RestAPI.Constantas;

public class WebhookPayload
{
    public string Event { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public object Data { get; set; } = new();
}
