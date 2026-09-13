namespace Smarthome.Shared.Events;

public record HeatingStateChangedEvent(
    Guid EventId,
    Guid HouseId,
    Guid? CommandId,
    string PreviousState,
    string CurrentState,
    string Source,
    DateTimeOffset ChangedAt)
{
    public const string RoutingKey = "smarthome.heating.state-changed";
}
