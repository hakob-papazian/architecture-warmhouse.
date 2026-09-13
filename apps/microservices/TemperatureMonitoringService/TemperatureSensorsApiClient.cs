using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace TemperatureMonitoringService;

public class SensorReading
{
    public double Value { get; set; }
    public string Unit { get; set; } = "C";
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("sensor_id")]
    public string SensorId { get; set; } = string.Empty;
}

public class TemperatureSensorsApiClient(HttpClient httpClient)
{
    public async Task<SensorReading?> GetReadingAsync(string sensorId, CancellationToken ct)
    {
        var response = await httpClient.GetAsync($"/temperature/{Uri.EscapeDataString(sensorId)}", ct);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<SensorReading>(cancellationToken: ct)
            : null;
    }
}
