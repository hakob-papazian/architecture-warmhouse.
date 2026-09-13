using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Smarthome.Shared.Messaging;

/// <summary>
/// Subscribes a durable queue to one routing key on the shared topic exchange and
/// invokes <see cref="HandleAsync"/> for each message. One subclass per event type
/// a service consumes (mirrors the "Event Consumer" component on the C4 diagrams).
/// </summary>
public abstract class EventConsumerBackgroundService<TEvent> : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly string _queueName;
    private readonly string _routingKey;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private IConnection? _connection;
    private IModel? _channel;

    protected EventConsumerBackgroundService(
        RabbitMqOptions options,
        string queueName,
        string routingKey,
        IServiceScopeFactory scopeFactory,
        ILogger logger)
    {
        _options = options;
        _queueName = queueName;
        _routingKey = routingKey;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected abstract Task HandleAsync(TEvent @event, IServiceProvider scopedServices, CancellationToken ct);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _connection = RabbitMqConnectionFactory.ConnectWithRetry(_options, _logger);
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_options.Exchange, ExchangeType.Topic, durable: true);
        _channel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_queueName, _options.Exchange, _routingKey);
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (_, args) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(args.Body.ToArray());
                var @event = JsonSerializer.Deserialize<TEvent>(json);
                if (@event is not null)
                {
                    using var scope = _scopeFactory.CreateScope();
                    HandleAsync(@event, scope.ServiceProvider, stoppingToken).GetAwaiter().GetResult();
                }

                _channel.BasicAck(args.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message from {Queue}, requeueing", _queueName);
                _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(_queueName, autoAck: false, consumer);

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
