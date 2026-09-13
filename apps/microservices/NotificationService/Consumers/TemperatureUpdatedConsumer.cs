using NotificationService.Data;
using NotificationService.Models;
using Smarthome.Shared.Events;
using Smarthome.Shared.Messaging;

namespace NotificationService.Consumers;

// "Notification Rules Engine": only an abnormal reading is worth alerting a
// user about - routine updates within the comfortable range are not.
public class TemperatureUpdatedConsumer(
    RabbitMqOptions options,
    IServiceScopeFactory scopeFactory,
    ILogger<TemperatureUpdatedConsumer> logger)
    : EventConsumerBackgroundService<TemperatureUpdatedEvent>(
        options,
        queueName: "notification-service.temperature-updated",
        routingKey: TemperatureUpdatedEvent.RoutingKey,
        scopeFactory,
        logger)
{
    private const double LowThreshold = 10.0;
    private const double HighThreshold = 30.0;

    protected override async Task HandleAsync(TemperatureUpdatedEvent @event, IServiceProvider scopedServices, CancellationToken ct)
    {
        if (@event.Value is > LowThreshold and < HighThreshold)
        {
            return;
        }

        var ownerClient = scopedServices.GetRequiredService<HouseOwnerClient>();
        var userId = await ownerClient.GetOwnerAsync(@event.HouseId, ct);
        if (userId is null)
        {
            return;
        }

        var db = scopedServices.GetRequiredService<NotificationDbContext>();
        db.Logs.Add(new NotificationLog
        {
            UserId = userId.Value,
            HouseId = @event.HouseId,
            EventType = "temperature_anomaly",
            Message = $"Unusual temperature reading: {@event.Value}{@event.Unit} (device {@event.DeviceId})",
        });
        await db.SaveChangesAsync(ct);
    }
}
