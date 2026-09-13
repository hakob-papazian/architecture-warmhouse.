using HeatingControlService.Models;
using Xunit;

namespace HeatingControlService.Tests;

public class HeatingProfileTests
{
    [Fact]
    public void TurnsOnWhenBelowTargetAndCurrentlyOff()
    {
        var profile = new HeatingProfile { Mode = HeatingMode.Auto, TargetTemperature = 21, CurrentState = HeatingState.Off };

        var action = profile.DecideAutoAction(observedTemperature: 18.5);

        Assert.Equal(HeatingState.On, action);
    }

    [Fact]
    public void TurnsOffWhenAtOrAboveTargetAndCurrentlyOn()
    {
        var profile = new HeatingProfile { Mode = HeatingMode.Auto, TargetTemperature = 21, CurrentState = HeatingState.On };

        var action = profile.DecideAutoAction(observedTemperature: 21.0);

        Assert.Equal(HeatingState.Off, action);
    }

    [Fact]
    public void NoActionWhenAlreadyOffAndAboveTarget()
    {
        var profile = new HeatingProfile { Mode = HeatingMode.Auto, TargetTemperature = 21, CurrentState = HeatingState.Off };

        var action = profile.DecideAutoAction(observedTemperature: 25.0);

        Assert.Null(action);
    }

    [Fact]
    public void NoActionWhenAlreadyOnAndBelowTarget()
    {
        var profile = new HeatingProfile { Mode = HeatingMode.Auto, TargetTemperature = 21, CurrentState = HeatingState.On };

        var action = profile.DecideAutoAction(observedTemperature: 18.0);

        Assert.Null(action);
    }

    [Fact]
    public void NeverActsInManualMode()
    {
        var profile = new HeatingProfile { Mode = HeatingMode.Manual, TargetTemperature = 21, CurrentState = HeatingState.Off };

        var action = profile.DecideAutoAction(observedTemperature: 5.0);

        Assert.Null(action);
    }
}
