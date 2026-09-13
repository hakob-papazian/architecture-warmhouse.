namespace TemperatureMonitoringService;

public record WatchSensorRequest(string SensorId, Guid DeviceId);
public record TelemetryResponse(Guid DeviceId, double Value, string Unit, DateTimeOffset RecordedAt);
