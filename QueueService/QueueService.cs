namespace aspnet_core_azureservicebus.QueueService;

using Azure.Messaging.ServiceBus;
using System.Text.Json;

/// <summary>
/// Service responsible for sending and receiving messages using Azure Service Bus queues.
///
/// Service must register as a singleton.
/// </summary>
/// <param name="logger">Injected logger for logging operations</param>
/// <param name="serviceBusClient">Injected Azure ServiceBusClient used for queue operations</param>
public sealed class QueueService(
	ILogger<QueueService> logger,
	ServiceBusClient serviceBusClient
) : IQueueService, IAsyncDisposable
{
	/// <summary>
	/// Represents service bus processor.
	/// </summary>
	private ServiceBusProcessor? _processor;

	/// <summary>
	/// Represents JSON serializer options.
	/// </summary>
	private static readonly JsonSerializerOptions _jsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true
	};

	/// <summary>
	/// Sends a message to the specified queue.
	/// </summary>
	/// <typeparam name="T">Generic type</typeparam>
	/// <param name="queueName">Queue name</param>
	/// <param name="message">Message to send</param>
	/// <param name="cancellationToken">Cancellation token</param>
	public async Task SendMessageAsync<T>(
		string queueName,
		T message,
		CancellationToken cancellationToken)
	{
		// Check for null arguments.
		ArgumentNullException.ThrowIfNull(message);

		ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

		// Create a sender for the queue.
		await using ServiceBusSender sender = serviceBusClient.CreateSender(queueName);

		// Serialize the message to JSON.
		string jsonMessage = JsonSerializer.Serialize(message, _jsonOptions);

		// Send the message.
		await sender.SendMessageAsync(new ServiceBusMessage(jsonMessage), cancellationToken);
	}

	/// <summary>
	/// Receives a message from the specified queue.
	/// </summary>
	/// <typeparam name="T">Generic type</typeparam>
	/// <param name="queueName">Queue name</param>
	/// <param name="timeout">Time out</param>
	/// <param name="cancellationToken">Cancellation token</param>
	public async Task<T?> ReceiveMessageAsync<T>(
		string queueName,
		TimeSpan? timeout,
		CancellationToken cancellationToken)
	{
		// Check for null arguments.
		ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

		// Create a receiver for the queue.
		await using ServiceBusReceiver receiver = serviceBusClient.CreateReceiver(queueName);

		// Receive the message.
		ServiceBusReceivedMessage message = await receiver.ReceiveMessageAsync(timeout ?? TimeSpan.FromSeconds(5), cancellationToken);

		// If no message was received, return default.
		if (message is null) return default;

		try
		{
			T? result = JsonSerializer.Deserialize<T>(message.Body.ToString(), _jsonOptions);

			// Complete the message so that it is not received again.
			await receiver.CompleteMessageAsync(message, cancellationToken);

			// Deserialize the message body from JSON.
			return result;
		}
		catch
		{
			await receiver.AbandonMessageAsync(message, cancellationToken: cancellationToken);

			throw;
		}
	}

	/// <summary>
	/// Listens for messages on the specified queue and processes them using the provided message handler.
	/// </summary>
	/// <typeparam name="T">Generic type</typeparam>
	/// <param name="queueName">Queue name</param>
	/// <param name="handler">Message handler</param>
	/// <param name="cancellationToken">Cancellation token</param>
	public async Task StartListeningAsync<T>(
		string queueName,
		Func<T, CancellationToken, Task> handler,
		CancellationToken cancellationToken)
	{
		// Check for null arguments.
		ArgumentNullException.ThrowIfNull(handler);

		ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

		await StopListeningAsync();

		// Create options.
		ServiceBusProcessorOptions options = new()
		{
			PrefetchCount = 10,
			MaxConcurrentCalls = 5,
			AutoCompleteMessages = false
		};

		// Create a processor.
		_processor = serviceBusClient.CreateProcessor(queueName, options);

		// Process message.
		_processor.ProcessMessageAsync += async args =>
		{
			try
			{
				// Dead letter invalid messages.
				if (JsonSerializer.Deserialize<T>(args.Message.Body.ToString(), _jsonOptions) is not { } payload)
				{
					if (logger.IsEnabled(LogLevel.Warning))
						logger.LogWarning("Invalid message received. Dead-lettering.");

					await args.DeadLetterMessageAsync(
						args.Message,
						deadLetterReason: "InvalidPayload",
						deadLetterErrorDescription: $"Unable to deserialize message to {typeof(T).Name}"
					);

					return;
				}

				await handler(payload, args.CancellationToken);

				await args.CompleteMessageAsync(args.Message, args.CancellationToken);
			}
			catch (Exception ex)
			{
				if (logger.IsEnabled(LogLevel.Error))
					logger.LogError(ex, "An error occurred while processing the message. Abandoning message.");

				await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
			}
		};

		// Process error.
		_processor.ProcessErrorAsync += args =>
		{
			if (logger.IsEnabled(LogLevel.Error))
				logger.LogError(args.Exception, "Service Bus processor error: EntityPath: {EntityPath}", args.EntityPath);

			return Task.CompletedTask;
		};

		// Start processing.
		await _processor.StartProcessingAsync(cancellationToken);
	}

	/// <summary>
	/// Stops listening and disposes resources asynchronously.
	/// </summary>
	public async Task StopListeningAsync()
	{
		if (_processor is null) return;

		await _processor.StopProcessingAsync();

		await _processor.DisposeAsync();

		_processor = null;
	}

	/// <summary>
	/// Stops listening and disposes resources asynchronously.
	/// </summary>
	public async ValueTask DisposeAsync() => await StopListeningAsync();
}