using Microsoft.EntityFrameworkCore;
using Smarthome.Shared.Events;
using Smarthome.Shared.Messaging;
using TemperatureMonitoringService.Data;
using TemperatureMonitoringService.Models;

namespace TemperatureMonitoringService;

// The "Sensor Poller" component: periodically asks the external Temperature
// Sensors API (temperature-api, built for Task 5) for a fresh reading per
// watched sensor, persists it, and publishes TemperatureUpdated.
public class SensorPollerHostedService(
    IServiceScopeFactory scopeFactory,
    TemperatureSensorsApiClient sensorsApi,
    IEventPublisher eventPublisher,
    ILogger<SensorPollerHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            try
            {
                await PollOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sensor poll cycle failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PollOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TemperatureDbContext>();

        var watches = await db.WatchedSensors.ToListAsync(ct);
        foreach (var watch in watches)
        {
            var reading = await sensorsApi.GetReadingAsync(watch.SensorId, ct);
            if (reading is null)
            {
                logger.LogWarning("No reading returned for sensor {SensorId}", watch.SensorId);
                continue;
            }

            db.Readings.Add(new TelemetryReading
            {
                HouseId = watch.HouseId,
                DeviceId = watch.DeviceId,
                Value = reading.Value,
                Unit = reading.Unit,
                RecordedAt = reading.Timestamp,
            });
            await db.SaveChangesAsync(ct);

            eventPublisher.Publish(TemperatureUpdatedEvent.RoutingKey, new TemperatureUpdatedEvent(
                EventId: Guid.NewGuid(),
                HouseId: watch.HouseId,
                DeviceId: watch.DeviceId.ToString(),
                Value: reading.Value,
                Unit: reading.Unit,
                RecordedAt: reading.Timestamp));
        }
    }
}
