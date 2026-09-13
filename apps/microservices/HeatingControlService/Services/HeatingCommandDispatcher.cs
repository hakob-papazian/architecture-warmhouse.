using HeatingControlService.Data;
using HeatingControlService.Models;
using Microsoft.EntityFrameworkCore;
using Smarthome.Shared.Events;
using Smarthome.Shared.Messaging;

namespace HeatingControlService.Services;

public enum DispatchOutcome { Applied, Conflict, EquipmentUnreachable, HouseNotFound }

// The "Heating Command Dispatcher" component: applies a wanted heating state,
// talks to the equipment, persists the result and publishes HeatingStateChanged
// when the state actually flips. Used by both the manual REST endpoint and the
// automation event consumer, exactly as shown on the Task 2 sequence diagram.
public class HeatingCommandDispatcher(
    HeatingDbContext db,
    IHeatingEquipmentClient equipmentClient,
    HouseDirectoryClient houseDirectory,
    IEventPublisher eventPublisher,
    ILogger<HeatingCommandDispatcher> logger)
{
    // Returns null when houseId doesn't correspond to a real house (backs the
    // 404 responses in the OpenAPI spec). Only checks with User & Home Service
    // the first time a house is seen - once a profile exists, it's trusted.
    public async Task<HeatingProfile?> GetOrCreateProfileAsync(Guid houseId, CancellationToken ct)
    {
        var profile = await db.Profiles.FindAsync([houseId], ct);
        if (profile is null)
        {
            if (!await houseDirectory.HouseExistsAsync(houseId, ct))
            {
                return null;
            }

            profile = new HeatingProfile { HouseId = houseId };
            db.Profiles.Add(profile);
            await db.SaveChangesAsync(ct);
        }

        return profile;
    }

    public async Task<(DispatchOutcome Outcome, HeatingCommand? Command)> DispatchAsync(
        Guid houseId, HeatingState action, CommandSource source, CancellationToken ct)
    {
        var profile = await GetOrCreateProfileAsync(houseId, ct);
        if (profile is null)
        {
            return (DispatchOutcome.HouseNotFound, null);
        }

        if (source == CommandSource.Manual && profile.Mode == HeatingMode.Auto)
        {
            return (DispatchOutcome.Conflict, null);
        }

        var confirmed = await equipmentClient.SendAsync(houseId, action, ct);
        if (!confirmed)
        {
            return (DispatchOutcome.EquipmentUnreachable, null);
        }

        var previousState = profile.CurrentState;
        var command = new HeatingCommand { HouseId = houseId, Action = action, Source = source, Confirmed = true };
        db.Commands.Add(command);

        profile.CurrentState = action;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        if (previousState != action)
        {
            logger.LogInformation("Heating state changed for house {HouseId}: {Previous} -> {Current} ({Source})",
                houseId, previousState, action, source);

            eventPublisher.Publish(HeatingStateChangedEvent.RoutingKey, new HeatingStateChangedEvent(
                EventId: Guid.NewGuid(),
                HouseId: houseId,
                CommandId: command.Id,
                PreviousState: previousState.ToString().ToLowerInvariant(),
                CurrentState: action.ToString().ToLowerInvariant(),
                Source: source.ToString().ToLowerInvariant(),
                ChangedAt: DateTimeOffset.UtcNow));
        }

        return (DispatchOutcome.Applied, command);
    }
}
