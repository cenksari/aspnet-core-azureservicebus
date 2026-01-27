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
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("ServiceBus listener starting...");

        try
        {
            await queueService.StartListeningAsync<Message>(queueName, async message =>
            {
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("Message received width Id: {Id} and Message {Message}", message.Id, message.Text);
            },
            stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException oce)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation(oce, "ServiceBus listener is stopping due to cancellation.");
        }
        catch (Exception ex)
        {
            // Log any unexpected errors during the date deactivation process.
            if (logger.IsEnabled(LogLevel.Error))
                logger.LogError(ex, "Critical error in ServiceBus listener. {Message}", ex.Message);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await queueService.StopListeningAsync();

        await base.StopAsync(cancellationToken);
    }
}