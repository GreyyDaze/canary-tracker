using CanaryTracker.Data;
using CanaryTracker.Models;
using CanaryTracker.Services;

namespace CanaryTracker.Endpoints;

public static class CanaryEndpoints
{
    public static void MapCanaryEndpoints(this WebApplication app)
    {
        app.MapGet("/canaries", (AppDbContext db) =>
            Results.Content(RenderingService.RenderCanarySection(db), "text/html"));

        app.MapPost("/canaries", async (AppDbContext db, HttpContext ctx) =>
        {
            var form = await ctx.Request.ReadFormAsync();
            var name = form["name"].FirstOrDefault() ?? "";
            var categoryStr = form["category"].FirstOrDefault() ?? "";
            if (!Enum.TryParse<CanaryCategory>(categoryStr, out var category))
                return Results.BadRequest("Invalid category");

            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var baseDir = Path.Combine(homeDir, ".config", "canary");
            var dir = Path.Combine(baseDir, $"{name.ToLower().Replace(' ', '-')}-{DateTime.UtcNow:yyyyMMdd-HHmmss}");
            Directory.CreateDirectory(dir);
            CanaryWatcherService.MarkDeploy(dir);

            var (fileContent, fileName) = category switch
            {
                CanaryCategory.Cloud => (
                    $"[canary-{name}]\n" +
                    $"aws_access_key_id = AKIA{Random.Shared.Next(10000000, 99999999)}\n" +
                    $"aws_secret_access_key = {Convert.ToHexString(Random.Shared.GetItems("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"u8, 40))}\n" +
                    "region = us-east-1\n",
                    "credentials"),
                CanaryCategory.Infrastructure => (
                    "-----BEGIN OPENSSH PRIVATE KEY-----\n" +
                    string.Join('\n', Enumerable.Range(0, 10)
                        .Select(_ => Convert.ToHexString(Random.Shared.GetItems("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"u8, 64)))) +
                    "\n-----END OPENSSH PRIVATE KEY-----\n",
                    "id_rsa"),
                CanaryCategory.Credential => (
                    $"github.com:deploy-{name}:ghp_{Convert.ToHexString(Random.Shared.GetItems("abcdefghijklmnopqrstuvwxyz0123456789"u8, 36))}\n" +
                    $"gitlab.com:admin-{name}:glpat-{Convert.ToHexString(Random.Shared.GetItems("abcdefghijklmnopqrstuvwxyz0123456789"u8, 22))}\n" +
                    $"docker.com:ci-{name}:dckr_pat_{Convert.ToHexString(Random.Shared.GetItems("abcdefghijklmnopqrstuvwxyz0123456789"u8, 24))}\n",
                    "passwords.txt"),
                _ => ("", "")
            };

            await File.WriteAllTextAsync(Path.Combine(dir, fileName), fileContent);

            var canary = new Canary
            {
                Name = name,
                Category = category,
                DirectoryPath = dir,
                Status = "active",
                CreatedAt = DateTime.UtcNow
            };
            db.Canaries.Add(canary);
            await db.SaveChangesAsync();

            return Results.Content(RenderingService.RenderCanarySection(db), "text/html");
        });
    }
}
