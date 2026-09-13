namespace Smarthome.Shared.Messaging;

public interface IEventPublisher
{
    void Publish<TEvent>(string routingKey, TEvent @event);
}
