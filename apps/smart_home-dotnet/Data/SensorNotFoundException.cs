namespace SmartHome.Api.Data;

public class SensorNotFoundException : Exception
{
    public SensorNotFoundException(string message) : base(message)
    {
    }
}
