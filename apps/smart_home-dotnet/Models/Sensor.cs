using System.Text.Json.Serialization;

namespace SmartHome.Api.Models;

public class Sensor
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public double Value { get; set; }

    public string? Unit { get; set; }

    public string Status { get; set; } = "inactive";

    [JsonPropertyName("last_updated")]
    public DateTimeOffset LastUpdated { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}
