namespace CanaryTracker.Models;

public enum CanaryCategory { Cloud, Infrastructure, Credential }

public class Canary
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public CanaryCategory Category { get; set; }
    public required string DirectoryPath { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastDetectedAt { get; set; }
}
