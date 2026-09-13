namespace SmartHome.Api.Services;

public interface ITemperatureService
{
    Task<TemperatureResponse> GetTemperatureAsync(string location, CancellationToken ct = default);

    Task<TemperatureResponse> GetTemperatureByIdAsync(string sensorId, CancellationToken ct = default);
}
