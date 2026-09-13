using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

string GetEnv(string key, string defaultValue) =>
    Environment.GetEnvironmentVariable(key) is { Length: > 0 } value ? value : defaultValue;

var port = GetEnv("PORT", "8081").TrimStart(':');
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/temperature", (string? location, string? sensorId) =>
{
    var (resolvedLocation, resolvedSensorId) = SensorLocations.Resolve(location, sensorId);
    return Results.Ok(TemperatureReadingFactory.Create(resolvedLocation, resolvedSensorId));
});

app.MapGet("/temperature/{sensorId}", (string sensorId) =>
{
    var (resolvedLocation, resolvedSensorId) = SensorLocations.Resolve(location: null, sensorId);
    return Results.Ok(TemperatureReadingFactory.Create(resolvedLocation, resolvedSensorId));
});

app.Logger.LogInformation("Temperature API listening on port {Port}", port);
app.Run();

// Mirrors the location <-> sensorId defaulting rules from the assignment spec.
static class SensorLocations
{
    private static readonly Dictionary<string, string> IdToLocation = new()
    {
        ["1"] = "Living Room",
        ["2"] = "Bedroom",
        ["3"] = "Kitchen",
    };

    private static readonly Dictionary<string, string> LocationToId = new()
    {
        ["Living Room"] = "1",
        ["Bedroom"] = "2",
        ["Kitchen"] = "3",
    };

    public static (string Location, string SensorId) Resolve(string? location, string? sensorId)
    {
        if (string.IsNullOrEmpty(location))
        {
            location = sensorId is not null && IdToLocation.TryGetValue(sensorId, out var loc)
                ? loc
                : "Unknown";
        }

        if (string.IsNullOrEmpty(sensorId))
        {
            sensorId = LocationToId.TryGetValue(location, out var id) ? id : "0";
        }

        return (location, sensorId);
    }
}

static class TemperatureReadingFactory
{
    public static TemperatureReading Create(string location, string sensorId) => new(
        Value: Math.Round(Random.Shared.NextDouble() * (30.0 - 15.0) + 15.0, 1),
        Unit: "C",
        Timestamp: DateTimeOffset.UtcNow,
        Location: location,
        Status: "ok",
        SensorId: sensorId,
        SensorType: "temperature",
        Description: $"Simulated reading for {location}");
}

record TemperatureReading(
    double Value,
    string Unit,
    DateTimeOffset Timestamp,
    string Location,
    string Status,
    [property: JsonPropertyName("sensor_id")] string SensorId,
    [property: JsonPropertyName("sensor_type")] string SensorType,
    string Description);
