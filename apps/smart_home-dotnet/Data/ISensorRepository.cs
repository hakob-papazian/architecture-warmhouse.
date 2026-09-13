using SmartHome.Api.Models;
using SmartHome.Api.Models.Dtos;

namespace SmartHome.Api.Data;

public interface ISensorRepository
{
    Task<List<Sensor>> GetSensorsAsync(CancellationToken ct = default);

    Task<Sensor> GetSensorByIdAsync(int id, CancellationToken ct = default);

    Task<Sensor> CreateSensorAsync(SensorCreateDto dto, CancellationToken ct = default);

    Task<Sensor> UpdateSensorAsync(int id, SensorUpdateDto dto, CancellationToken ct = default);

    Task DeleteSensorAsync(int id, CancellationToken ct = default);

    Task UpdateSensorValueAsync(int id, double value, string status, CancellationToken ct = default);
}
