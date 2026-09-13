using NotificationService.Data;
using NotificationService.Models;
using Smarthome.Shared.Events;
using Smarthome.Shared.Messaging;

namespace NotificationService.Consumers;

public class HeatingStateChangedConsumer(
    RabbitMqOptions options,
    IServiceScopeFactory scopeFactory,
    ILogger<HeatingStateChangedConsumer> logger)
    : EventConsumerBackgroundService<HeatingStateChangedEvent>(
        options,
        queueName: "notification-service.heating-state-changed",
        routingKey: HeatingStateChangedEvent.RoutingKey,
        scopeFactory,
        logger)
{
    protected override async Task HandleAsync(HeatingStateChangedEvent @event, IServiceProvider scopedServices, CancellationToken ct)
    {
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
            EventType = "heating_state_changed",
            Message = $"Heating turned {@event.CurrentState} ({@event.Source})",
        });
        await db.SaveChangesAsync(ct);
    }
}
