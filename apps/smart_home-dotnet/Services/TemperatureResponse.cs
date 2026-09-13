using System.Text.Json.Serialization;

namespace SmartHome.Api.Services;

public class TemperatureResponse
{
    public double Value { get; set; }

    public string Unit { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; }

    public string Location { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("sensor_id")]
    public string SensorId { get; set; } = string.Empty;

    [JsonPropertyName("sensor_type")]
    public string SensorType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
