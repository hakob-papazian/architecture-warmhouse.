namespace HeatingControlService.Models;

public enum HeatingMode { Manual, Auto }
public enum HeatingState { On, Off }

// Mirrors the HomeHeatingProfile class from the Task 2 code-level class diagram.
public class HeatingProfile
{
    public Guid HouseId { get; set; }
    public HeatingMode Mode { get; set; } = HeatingMode.Auto;
    public double TargetTemperature { get; set; } = 21.0;
    public HeatingState CurrentState { get; set; } = HeatingState.Off;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Decides whether a new temperature reading should flip the heating state.
    // Only called when Mode == Auto; returns null when no change is needed.
    public HeatingState? DecideAutoAction(double observedTemperature)
    {
        if (Mode != HeatingMode.Auto)
        {
            return null;
        }

        if (observedTemperature < TargetTemperature && CurrentState == HeatingState.Off)
        {
            return HeatingState.On;
        }

        if (observedTemperature >= TargetTemperature && CurrentState == HeatingState.On)
        {
            return HeatingState.Off;
        }

        return null;
    }
}
