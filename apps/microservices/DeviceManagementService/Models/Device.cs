namespace DeviceManagementService.Models;

public class Device
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TypeId { get; set; }
    public Guid HouseId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = "enabled";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DeviceType? Type { get; set; }
}
