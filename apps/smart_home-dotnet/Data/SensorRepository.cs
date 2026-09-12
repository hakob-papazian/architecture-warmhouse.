using Microsoft.EntityFrameworkCore;
using SmartHome.Api.Models;
using SmartHome.Api.Models.Dtos;

namespace SmartHome.Api.Data;

public class SensorRepository : ISensorRepository
{
    private readonly SmartHomeDbContext _context;

    public SensorRepository(SmartHomeDbContext context)
    {
        _context = context;
    }

    public async Task<List<Sensor>> GetSensorsAsync(CancellationToken ct = default)
    {
        return await _context.Sensors
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .ToListAsync(ct);
    }

    public async Task<Sensor> GetSensorByIdAsync(int id, CancellationToken ct = default)
    {
        var sensor = await _context.Sensors.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (sensor is null)
        {
            throw new SensorNotFoundException($"error getting sensor by ID: sensor {id} not found");
        }

        return sensor;
    }

    public async Task<Sensor> CreateSensorAsync(SensorCreateDto dto, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var sensor = new Sensor
        {
            Name = dto.Name,
            Type = dto.Type,
            Location = dto.Location,
            Unit = dto.Unit ?? string.Empty,
            Status = "inactive",
            Value = 0,
            LastUpdated = now,
            CreatedAt = now,
        };

        _context.Sensors.Add(sensor);
        await _context.SaveChangesAsync(ct);

        return sensor;
    }

    public async Task<Sensor> UpdateSensorAsync(int id, SensorUpdateDto dto, CancellationToken ct = default)
    {
        var sensor = await _context.Sensors.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (sensor is null)
        {
            throw new SensorNotFoundException($"error getting sensor by ID: sensor {id} not found");
        }

        sensor.LastUpdated = DateTimeOffset.UtcNow;

        if (!string.IsNullOrEmpty(dto.Name))
        {
            sensor.Name = dto.Name;
        }

        if (!string.IsNullOrEmpty(dto.Type))
        {
            sensor.Type = dto.Type;
        }

        if (!string.IsNullOrEmpty(dto.Location))
        {
            sensor.Location = dto.Location;
        }

        if (dto.Value.HasValue)
        {
            sensor.Value = dto.Value.Value;
        }

        if (!string.IsNullOrEmpty(dto.Unit))
        {
            sensor.Unit = dto.Unit;
        }

        if (!string.IsNullOrEmpty(dto.Status))
        {
            sensor.Status = dto.Status;
        }

        await _context.SaveChangesAsync(ct);

        return sensor;
    }

    public async Task DeleteSensorAsync(int id, CancellationToken ct = default)
    {
        var rowsAffected = await _context.Sensors.Where(s => s.Id == id).ExecuteDeleteAsync(ct);
        if (rowsAffected == 0)
        {
            throw new SensorNotFoundException("sensor not found");
        }
    }

    public async Task UpdateSensorValueAsync(int id, double value, string status, CancellationToken ct = default)
    {
        var rowsAffected = await _context.Sensors
            .Where(s => s.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Value, value)
                .SetProperty(s => s.Status, status)
                .SetProperty(s => s.LastUpdated, DateTimeOffset.UtcNow), ct);

        if (rowsAffected == 0)
        {
            throw new SensorNotFoundException("sensor not found");
        }
    }
}
