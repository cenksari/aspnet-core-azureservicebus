# ASP.NET Core Azure Service Bus

**Reliable Messaging with ASP.NET Core & Azure Service Bus**

Welcome to **ASP.NET Core Azure Service Bus**, a simple and practical helper designed to demonstrate how to send and consume messages using **Azure Service Bus** in an ASP.NET Core application.

This project provides a clean example of integrating Azure Service Bus with:
- An HTTP API for sending messages
- A background service that consumes messages immediately

It is suitable for learning purposes, prototyping, and as a solid starting point for real-world messaging-based architectures.

## Key Features

- **Azure Service Bus Queue Integration**  
  Send and receive messages using Azure Service Bus queues.

- **HTTP Endpoint for Message Publishing**  
  Publish messages to the service bus via a REST API.

- **Background Message Consumer**  
  Messages are consumed instantly using an ASP.NET Core `BackgroundService`.

- **ASP.NET Core Native Approach**  
  Uses built-in dependency injection and hosted services.

- **Clean & Extensible Structure**  
  Easy to extend for production scenarios (logging, retries, DLQ, etc.).

## How It Works

1. A client sends an HTTP POST request to the API  
2. The API publishes the message to Azure Service Bus  
3. A background service listens to the queue  
4. The message is consumed and processed immediately

This demonstrates a basic **producer–consumer** pattern using Azure Service Bus.

## Installation

1. Clone the repository:

```bash
git clone https://github.com/cenksari/aspnet-core-azureservicebus.git
```

2. Open the project with **Visual Studio**

3. Configure Azure Service Bus settings in `appsettings.json`:

```json
{
  "ServiceBus": {
    "ConnectionString": "YOUR_AZURE_SERVICE_BUS_CONNECTION_STRING",
  }
}
```

4. Restore dependencies and run the application

## Usage

Once the application is running, send a POST request to the following endpoint:

```http
POST https://localhost:7123/send
```

### Request Body

```json
{
  "id": 1,
  "text": "Service bus message!"
}
```

### What Happens Next?

- The API sends the message to **Azure Service Bus**
- The **background service** consumes the message immediately
- Message processing can be observed via logs or debugger output

## Example Use Cases

- Event-driven architectures  
- Background processing  
- Decoupling services  
- Azure Service Bus learning projects  
- Starter template for enterprise messaging systems  

## Contributing

If you would like to contribute, please create a new branch and submit a pull request with your changes. Review may be needed before acceptance.

## Authors

@cenksari

## License

MIT
