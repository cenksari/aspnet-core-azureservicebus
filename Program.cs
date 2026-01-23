using aspnet_core_azureservicebus.Models;
using aspnet_core_azureservicebus.QueueService;
using aspnet_core_azureservicebus.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddAzureClients(options =>
{
    options.AddServiceBusClient(builder.Configuration["ServiceBus:ConnectionString"]);
});

builder.Services.AddSingleton<IQueueService, QueueService>();

builder.Services.AddHostedService<ServiceBusService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapPost("/send", async (
    IQueueService queueService,
    [FromBody] Message message
) =>
{
    if (string.IsNullOrEmpty(message.Text))
        return Results.BadRequest(
            new
            {
                Status = "Message failed",
                Error = "Please provide a message!"
            }
        );

    await queueService.SendMessageAsync("test-queue", message);

    return Results.Ok(
        new
        {
            message.Id,
            message.Text,
            Status = "Message sent"
        }
    );
});

await app.RunAsync();