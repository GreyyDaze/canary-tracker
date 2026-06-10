using System.Collections.Concurrent;
using System.Diagnostics;
using CanaryTracker.Data;
using CanaryTracker.Models;

namespace CanaryTracker.Services;

public class CanaryWatcherService(IServiceProvider services) : BackgroundService
{
    private static readonly string BasePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "canary");

    private static readonly ConcurrentDictionary<string, DateTime> _deployDirs = new();
    private static readonly ConcurrentDictionary<string, DateTime> _recentEvents = new();

    public static void MarkDeploy(string dirPath) =>
        _deployDirs[dirPath] = DateTime.UtcNow;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(BasePath);

        var watcher = new FileSystemWatcher(BasePath)
        {
            EnableRaisingEvents = true,
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
        };

        watcher.Changed += (_, e) => _ = OnFileEvent(e.FullPath, "modified", stoppingToken);
        watcher.Deleted += (_, e) => _ = OnFileEvent(e.FullPath, "deleted", stoppingToken);
        watcher.Renamed += (_, e) => _ = OnFileEvent(e.FullPath, "renamed", stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OnFileEvent(string fullPath, string action, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(fullPath);
        if (dir == null || !dir.StartsWith(BasePath)) return;

        if (_deployDirs.TryGetValue(dir, out var deployed) &&
            (DateTime.UtcNow - deployed).TotalSeconds < 1)
            return;

        var key = $"{fullPath}:{action}";
        var now = DateTime.UtcNow;

        if (_recentEvents.TryGetValue(key, out var last) && (now - last).TotalSeconds < 5)
            return;

        _recentEvents[key] = now;

        await Task.Delay(800, ct);
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var canary = db.Canaries.FirstOrDefault(c => c.DirectoryPath == dir);
        if (canary == null) return;

        var procInfo = await GetProcessInfo(fullPath);

        var source = procInfo ?? $"{action} ({Environment.UserName})";
        var message = action switch
        {
            "modified" => $"File modified — possible tampering by {procInfo ?? "unknown process"}",
            "deleted" => $"File deleted — possible exfiltration by {procInfo ?? "unknown process"}",
            "renamed" => $"File renamed — possible reconnaissance by {procInfo ?? "unknown process"}",
            _ => $"File was {action}"
        };

        db.Alerts.Add(new Alert
        {
            CanaryId = canary.Id,
            CanaryName = canary.Name,
            Message = message,
            Source = source,
            Status = AlertStatus.New,
            DetectedAt = DateTime.UtcNow
        });
        canary.Status = "compromised";
        canary.LastDetectedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private static async Task<string?> GetProcessInfo(string filePath)
    {
        try
        {
            var psi = new ProcessStartInfo("lsof", filePath)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var proc = Process.Start(psi);
            if (proc == null) return null;

            var output = await proc.StandardOutput.ReadToEndAsync();
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines.Skip(1))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                    return $"{parts[0]} (PID {parts[1]}, user: {parts[2]})";
            }
        }
        catch { }
        return null;
    }
}
