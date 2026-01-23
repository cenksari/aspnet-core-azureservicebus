namespace aspnet_core_azureservicebus.Models;

using System.Text.Json.Serialization;

public record Message
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}