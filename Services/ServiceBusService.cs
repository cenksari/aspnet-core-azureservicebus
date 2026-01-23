namespace aspnet_core_azureservicebus.Services;

using Models;
using QueueService;

public class ServiceBusService(
    IQueueService queueService,
    ILogger<ServiceBusService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Continuously run the logic until cancellation is requested.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await queueService.StartListeningAsync<Message>("test-queue", async message =>
                {
                    if (logger.IsEnabled(LogLevel.Information))
                        logger.LogInformation("Message received width Id: {Id} : Message {Message}", message.Id, message.Text);

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