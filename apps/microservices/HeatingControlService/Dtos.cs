using HeatingControlService.Models;

namespace HeatingControlService;

public record HeatingProfileResponse(Guid HouseId, string Mode, double TargetTemperature, string CurrentState, DateTimeOffset UpdatedAt)
{
    public static HeatingProfileResponse From(HeatingProfile p) => new(
        p.HouseId, p.Mode.ToString().ToLowerInvariant(), p.TargetTemperature, p.CurrentState.ToString().ToLowerInvariant(), p.UpdatedAt);
}

public record HeatingCommandResponse(Guid Id, Guid HouseId, string Action, string Source, DateTimeOffset IssuedAt, bool Confirmed)
{
    public static HeatingCommandResponse From(HeatingCommand c) => new(
        c.Id, c.HouseId, c.Action.ToString().ToLowerInvariant(), c.Source.ToString().ToLowerInvariant(), c.IssuedAt, c.Confirmed);
}

public record UpdateTargetTemperatureRequest(double TargetTemperature);
public record UpdateModeRequest(string Mode);
public record SendCommandRequest(string Action);
