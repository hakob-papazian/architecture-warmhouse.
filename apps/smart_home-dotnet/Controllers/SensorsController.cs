using Microsoft.AspNetCore.Mvc;
using SmartHome.Api.Data;
using SmartHome.Api.Models;
using SmartHome.Api.Models.Dtos;
using SmartHome.Api.Services;

namespace SmartHome.Api.Controllers;

[ApiController]
[Route("api/v1/sensors")]
public class SensorsController : ControllerBase
{
    private readonly ISensorRepository _repository;
    private readonly ITemperatureService _temperatureService;
    private readonly ILogger<SensorsController> _logger;

    public SensorsController(
        ISensorRepository repository,
        ITemperatureService temperatureService,
        ILogger<SensorsController> logger)
    {
        _repository = repository;
        _temperatureService = temperatureService;
        _logger = logger;
    }

    // GET /api/v1/sensors
    [HttpGet]
    public async Task<IActionResult> GetSensors(CancellationToken ct)
    {
        var sensors = await _repository.GetSensorsAsync(ct);

        // Update temperature sensors with real-time data from the external API
        foreach (var sensor in sensors.Where(s => s.Type == SensorTypes.Temperature))
        {
            try
            {
                var tempData = await _temperatureService.GetTemperatureByIdAsync(sensor.Id.ToString(), ct);
                sensor.Value = tempData.Value;
                sensor.Status = tempData.Status;
                sensor.LastUpdated = tempData.Timestamp;
                _logger.LogInformation("Updated temperature data for sensor {SensorId} from external API", sensor.Id);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Failed to fetch temperature data for sensor {SensorId}: {Error}", sensor.Id, ex.Message);
            }
        }

        return Ok(sensors);
    }

    // GET /api/v1/sensors/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSensorById(int id, CancellationToken ct)
    {
        Sensor sensor;
        try
        {
            sensor = await _repository.GetSensorByIdAsync(id, ct);
        }
        catch (SensorNotFoundException)
        {
            return NotFound(new { error = "Sensor not found" });
        }

        if (sensor.Type == SensorTypes.Temperature)
        {
            try
            {
                var tempData = await _temperatureService.GetTemperatureByIdAsync(sensor.Id.ToString(), ct);
                sensor.Value = tempData.Value;
                sensor.Status = tempData.Status;
                sensor.LastUpdated = tempData.Timestamp;
                _logger.LogInformation("Updated temperature data for sensor {SensorId} from external API", sensor.Id);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Failed to fetch temperature data for sensor {SensorId}: {Error}", sensor.Id, ex.Message);
            }
        }

        return Ok(sensor);
    }

    // GET /api/v1/sensors/temperature/{location}
    [HttpGet("temperature/{location}")]
    public async Task<IActionResult> GetTemperatureByLocation(string location, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(location))
        {
            return BadRequest(new { error = "Location is required" });
        }

        try
        {
            var tempData = await _temperatureService.GetTemperatureAsync(location, ct);
            return Ok(new
            {
                location = tempData.Location,
                value = tempData.Value,
                unit = tempData.Unit,
                status = tempData.Status,
                timestamp = tempData.Timestamp,
                description = tempData.Description,
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Failed to fetch temperature data: {ex.Message}" });
        }
    }

    // POST /api/v1/sensors
    [HttpPost]
    public async Task<IActionResult> CreateSensor([FromBody] SensorCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { error = "invalid request body" });
        }

        var sensor = await _repository.CreateSensorAsync(dto, ct);
        return CreatedAtAction(nameof(GetSensorById), new { id = sensor.Id }, sensor);
    }

    // PUT /api/v1/sensors/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSensor(int id, [FromBody] SensorUpdateDto dto, CancellationToken ct)
    {
        try
        {
            var sensor = await _repository.UpdateSensorAsync(id, dto, ct);
            return Ok(sensor);
        }
        catch (SensorNotFoundException ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // DELETE /api/v1/sensors/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSensor(int id, CancellationToken ct)
    {
        try
        {
            await _repository.DeleteSensorAsync(id, ct);
            return Ok(new { message = "Sensor deleted successfully" });
        }
        catch (SensorNotFoundException ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // PATCH /api/v1/sensors/{id}/value
    [HttpPatch("{id:int}/value")]
    public async Task<IActionResult> UpdateSensorValue(int id, [FromBody] SensorValueUpdateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { error = "value and status are required" });
        }

        try
        {
            await _repository.UpdateSensorValueAsync(id, dto.Value!.Value, dto.Status, ct);
            return Ok(new { message = "Sensor value updated successfully" });
        }
        catch (SensorNotFoundException ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
