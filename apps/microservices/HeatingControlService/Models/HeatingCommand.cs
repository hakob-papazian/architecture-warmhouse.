namespace HeatingControlService.Models;

public enum CommandSource { Manual, Auto }

public class HeatingCommand
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HouseId { get; set; }
    public HeatingState Action { get; set; }
    public CommandSource Source { get; set; }
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool Confirmed { get; set; }
}
