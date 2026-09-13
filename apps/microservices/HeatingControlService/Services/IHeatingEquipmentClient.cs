using HeatingControlService.Models;

namespace HeatingControlService.Services;

public interface IHeatingEquipmentClient
{
    Task<bool> SendAsync(Guid houseId, HeatingState action, CancellationToken ct);
}

// The real boiler/controller (System_Ext "Оборудование отопления" on the C4
// diagrams) isn't reachable from this environment, so this simulates the
// synchronous command + acknowledgement described in the Task 2 sequence
// diagram: a short delay, then success.
public class SimulatedHeatingEquipmentClient(ILogger<SimulatedHeatingEquipmentClient> logger) : IHeatingEquipmentClient
{
    public async Task<bool> SendAsync(Guid houseId, HeatingState action, CancellationToken ct)
    {
        logger.LogInformation("Simulated equipment command: house {HouseId} -> {Action}", houseId, action);
        await Task.Delay(200, ct);
        return true;
    }
}
