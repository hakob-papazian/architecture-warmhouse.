namespace DeviceManagementService.Models;

// A logical sub-component of a physical device (e.g. a hub exposing several
// sensor/actuator channels). Named explicitly in the Task 3 assignment text
// as a required example entity.
public class Module
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public string ModuleType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "enabled";

    public Device? Device { get; set; }
}
