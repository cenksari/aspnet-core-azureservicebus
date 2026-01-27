namespace aspnet_core_azureservicebus.QueueService;

public interface IQueueService
{
    Task StopListeningAsync();

    Task SendMessageAsync<T>(string queueName, T serviceBusMessage, CancellationToken cancellationToken = default);

    Task<T?> ReceiveMessageAsync<T>(string queueName, TimeSpan? timeout, CancellationToken cancellationToken = default);

    Task StartListeningAsync<T>(string queueName, Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default);
}