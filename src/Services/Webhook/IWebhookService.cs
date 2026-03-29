namespace RestAPI.Services.Webhook;

public interface IWebhookService
{
    Task SendAsync(string eventName, object data);
}
