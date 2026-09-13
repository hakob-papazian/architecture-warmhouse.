namespace NotificationService.Models;

public class NotificationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid HouseId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
    public string Status { get; set; } = "sent";
}
