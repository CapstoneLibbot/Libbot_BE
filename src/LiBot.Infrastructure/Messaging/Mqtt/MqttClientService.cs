namespace LiBot.Infrastructure.Messaging.Mqtt;

public class MqttClientService
{
    public Task PublishAsync(string topic, string payload, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return Task.CompletedTask;
    }
}
