namespace aspnet_core_azureservicebus.QueueService;

public interface IQueueService
{
    Task StopListeningAsync();

    Task SendMessageAsync<T>(string queueName, T message, CancellationToken cancellationToken);

    Task<T?> ReceiveMessageAsync<T>(string queueName, TimeSpan? timeout, CancellationToken cancellationToken);

    Task StartListeningAsync<T>(string queueName, Func<T, Task> messageHandler, CancellationToken cancellationToken);
}