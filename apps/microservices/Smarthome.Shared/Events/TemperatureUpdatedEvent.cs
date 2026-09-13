namespace Smarthome.Shared.Events;

public record TemperatureUpdatedEvent(
    Guid EventId,
    Guid HouseId,
    string DeviceId,
    double Value,
    string Unit,
    DateTimeOffset RecordedAt)
{
    public const string RoutingKey = "smarthome.temperature.updated";
}
