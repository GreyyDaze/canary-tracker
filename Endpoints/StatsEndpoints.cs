using CanaryTracker.Data;

namespace CanaryTracker.Endpoints;

public static class StatsEndpoints
{
    public static void MapStatsEndpoints(this WebApplication app)
    {
        app.MapGet("/stats", (AppDbContext db) =>
            Results.Content(Services.RenderingService.RenderStats(db), "text/html"));
    }
}
