namespace TemperatureMonitoringService.Models;

public class TelemetryReading
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HouseId { get; set; }
    public Guid DeviceId { get; set; }
    public double Value { get; set; }
    public string Unit { get; set; } = "C";
    public DateTimeOffset RecordedAt { get; set; }
}
