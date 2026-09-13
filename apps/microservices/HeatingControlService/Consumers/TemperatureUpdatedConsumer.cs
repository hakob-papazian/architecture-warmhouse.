using HeatingControlService.Models;
using HeatingControlService.Services;
using Smarthome.Shared.Events;
using Smarthome.Shared.Messaging;

namespace HeatingControlService.Consumers;

// The "Automation Handler" component: reacts to every TemperatureUpdated event
// and, when the house is in Auto mode, decides whether to flip the heating
// state (see the Task 2 code-level sequence diagram for this exact flow).
public class TemperatureUpdatedConsumer(
    RabbitMqOptions options,
    IServiceScopeFactory scopeFactory,
    ILogger<TemperatureUpdatedConsumer> logger)
    : EventConsumerBackgroundService<TemperatureUpdatedEvent>(
        options,
        queueName: "heating-control-service.temperature-updated",
        routingKey: TemperatureUpdatedEvent.RoutingKey,
        scopeFactory,
        logger)
{
    protected override async Task HandleAsync(TemperatureUpdatedEvent @event, IServiceProvider scopedServices, CancellationToken ct)
    {
        var dispatcher = scopedServices.GetRequiredService<HeatingCommandDispatcher>();
        var profile = await dispatcher.GetOrCreateProfileAsync(@event.HouseId, ct);

        var wantedAction = profile.DecideAutoAction(@event.Value);
        if (wantedAction is null)
        {
            return;
        }

        await dispatcher.DispatchAsync(@event.HouseId, wantedAction.Value, CommandSource.Auto, ct);
    }
}
