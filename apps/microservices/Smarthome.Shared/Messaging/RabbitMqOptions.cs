namespace Smarthome.Shared.Messaging;

public class RabbitMqOptions
{
    public string HostName { get; set; } = "rabbitmq";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "smarthome.events";

    public static RabbitMqOptions FromEnvironment()
    {
        string Get(string key, string fallback) =>
            Environment.GetEnvironmentVariable(key) is { Length: > 0 } value ? value : fallback;

        return new RabbitMqOptions
        {
            HostName = Get("RABBITMQ_HOST", "rabbitmq"),
            Port = int.TryParse(Get("RABBITMQ_PORT", "5672"), out var port) ? port : 5672,
            UserName = Get("RABBITMQ_USER", "guest"),
            Password = Get("RABBITMQ_PASSWORD", "guest"),
            Exchange = Get("RABBITMQ_EXCHANGE", "smarthome.events"),
        };
    }
}
