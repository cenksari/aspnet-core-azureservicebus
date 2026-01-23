namespace aspnet_core_azureservicebus.Services;

using Models;
using QueueService;

/// <summary>
/// Consumes ServiceBus queue.
/// </summary>
/// <param name="queueService">Queue service</param>
/// <param name="configuration">Configuration</param>
/// <param name="logger">Logger</param>
public class ServiceBusService(
    IQueueService queueService,
    IConfiguration configuration,
    ILogger<ServiceBusService> logger
) : BackgroundService
{
    /// <summary>
    /// The name of the queue for processing.
    /// </summary>
    private readonly string queueName = configuration["ServiceBus:QueueName"]
        ?? throw new MissingFieldException("ServiceBus queue name configuration not found!");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Continuously run the logic until cancellation is requested.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await queueService.StartListeningAsync<Message>(queueName, async message =>
                {
                    if (logger.IsEnabled(LogLevel.Information))
                        logger.LogInformation("Message received width Id: {Id} and Message {Message}", message.Id, message.Text);

                    await Task.CompletedTask;
                });

                stoppingToken.Register(async () => await queueService.StopListeningAsync());

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Gracefully handle cancellation (likely caused by stoppingToken)
            }
            catch (Exception ex)
            {
                // Log any unexpected errors during the date deactivation process.
                if (logger.IsEnabled(LogLevel.Error))
                    logger.LogError(ex, "An error occurred while deactivating dates. {Message}", ex.Message);
            }
        }
    }
}