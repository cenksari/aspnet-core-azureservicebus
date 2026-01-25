namespace aspnet_core_azureservicebus.QueueService;

using Azure.Messaging.ServiceBus;
using System.Text.Json;

/// <summary>
/// Service responsible for sending and receiving messages using Azure Service Bus queues.
/// </summary>
/// <param name="serviceBusClient">Injected Azure ServiceBusClient used for queue operations</param>
public class QueueService(
    ILogger<QueueService> logger,
    ServiceBusClient serviceBusClient
) : IQueueService
{
    /// <summary>
    /// Represents service bus processor.
    /// </summary>
    private ServiceBusProcessor? _processor;

    /// <summary>
    /// Sends a message to the specified queue.
    /// </summary>
    /// <typeparam name="T">Generic type</typeparam>
    /// <param name="queueName">Queue name</param>
    /// <param name="message">Message to send</param>
    public async Task SendMessageAsync<T>(string queueName, T message)
    {
        // Create a sender for the queue.
        ServiceBusSender sender = serviceBusClient.CreateSender(queueName);

        // Serialize the message to JSON.
        string? jsonMessage = JsonSerializer.Serialize(message);

        // Send the message.
        await sender.SendMessageAsync(new ServiceBusMessage(jsonMessage));
    }

    /// <summary>
    /// Receives a message from the specified queue.
    /// </summary>
    /// <typeparam name="T">Generic type</typeparam>
    /// <param name="queueName">Queue name</param>
    /// <param name="timeout">Time out</param>
    public async Task<T?> ReceiveMessageAsync<T>(string queueName, TimeSpan? timeout)
    {
        // Create a receiver for the queue.
        ServiceBusReceiver receiver = serviceBusClient.CreateReceiver(queueName);

        // Receive the message.
        ServiceBusReceivedMessage message = await receiver.ReceiveMessageAsync(timeout ?? TimeSpan.FromSeconds(5));

        // If no message was received, return default.
        if (message is null) return default;

        // Get the message body as a string.
        string? body = message.Body.ToString();

        try
        {
            // Deserialize message.
            T? result = JsonSerializer.Deserialize<T>(body);

            // Complete the message so that it is not received again.
            await receiver.CompleteMessageAsync(message);

            // Deserialize the message body from JSON.
            return result;
        }
        catch
        {
            // Abandon message.
            await receiver.AbandonMessageAsync(message);

            throw;
        }
    }

    /// <summary>
    /// Listens message queue.
    /// </summary>
    /// <typeparam name="T">Generic type</typeparam>
    /// <param name="queueName">Queue name</param>
    /// <param name="messageHandler">Message handler</param>
    public async Task StartListeningAsync<T>(string queueName, Func<T, Task> messageHandler)
    {
        if (_processor is not null)
            await StopListeningAsync();

        // Create a processor.
        _processor = serviceBusClient.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 1,
            AutoCompleteMessages = false
        });

        // Process message.
        _processor.ProcessMessageAsync += async args =>
        {
            try
            {
                string body = args.Message.Body.ToString();

                T? obj = JsonSerializer.Deserialize<T>(body);

                if (obj is not null)
                    await messageHandler(obj);

                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                    logger.LogError(ex, "An error occurred while processing the message. Abandoning message.");

                await args.AbandonMessageAsync(args.Message);
            }
        };

        // Process error.
        _processor.ProcessErrorAsync += args =>
        {
            if (logger.IsEnabled(LogLevel.Error))
                logger.LogError("Error processing message: {Message}", args.Exception.Message);

            return Task.CompletedTask;
        };

        // Start processing.
        await _processor.StartProcessingAsync();
    }

    /// <summary>
    /// Stops listening.
    /// </summary>
    public async Task StopListeningAsync()
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync();

            await _processor.DisposeAsync();
        }
    }
}