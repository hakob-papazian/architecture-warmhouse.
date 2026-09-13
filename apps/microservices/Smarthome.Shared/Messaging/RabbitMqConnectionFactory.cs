using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Smarthome.Shared.Messaging;

/// <summary>
/// Broker connections aren't guaranteed to be ready when a service container starts
/// (compose's depends_on only waits for the process to start, not for RabbitMQ to
/// finish booting), so the first connection attempt is retried with backoff.
/// </summary>
public static class RabbitMqConnectionFactory
{
    public static IConnection ConnectWithRetry(RabbitMqOptions options, ILogger logger, int maxAttempts = 10)
    {
        var factory = new ConnectionFactory
        {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            DispatchConsumersAsync = false,
        };

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return factory.CreateConnection();
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "RabbitMQ not ready yet (attempt {Attempt}/{MaxAttempts}), retrying...", attempt, maxAttempts);
                Thread.Sleep(TimeSpan.FromSeconds(Math.Min(attempt * 2, 15)));
            }
        }
    }
}
