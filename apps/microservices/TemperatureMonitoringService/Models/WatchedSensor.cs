namespace TemperatureMonitoringService.Models;

// Registered by a client (e.g. after creating a device in Device Management
// Service) so the Sensor Poller knows which external sensor id to keep polling
// for which house/device.
public class WatchedSensor
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HouseId { get; set; }
    public Guid DeviceId { get; set; }
    public string SensorId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
