namespace DeviceManagementService.Models;

public class DeviceType
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Unit { get; set; }
}
