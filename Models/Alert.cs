namespace CanaryTracker.Models;

public enum AlertStatus { New, Acknowledged, Resolved }

public class Alert
{
    public int Id { get; set; }
    public int CanaryId { get; set; }
    public required string CanaryName { get; set; }
    public required string Message { get; set; }
    public string Source { get; set; } = "";
    public AlertStatus Status { get; set; } = AlertStatus.New;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}
