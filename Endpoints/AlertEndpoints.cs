using CanaryTracker.Data;
using CanaryTracker.Models;
using CanaryTracker.Services;

namespace CanaryTracker.Endpoints;

public static class AlertEndpoints
{
    public static void MapAlertEndpoints(this WebApplication app)
    {
        app.MapGet("/alerts", (AppDbContext db) =>
            Results.Content(RenderingService.RenderAlerts(db), "text/html"));

        app.MapPost("/alerts/{id}/acknowledge", (AppDbContext db, int id) =>
        {
            var alert = db.Alerts.Find(id);
            if (alert == null) return Results.NotFound();
            alert.Status = AlertStatus.Acknowledged;
            db.SaveChanges();
            return Results.Content(RenderingService.RenderAlerts(db), "text/html");
        });

        app.MapPost("/alerts/{id}/resolve", (AppDbContext db, int id) =>
        {
            var alert = db.Alerts.Find(id);
            if (alert == null) return Results.NotFound();
            alert.Status = AlertStatus.Resolved;
            db.SaveChanges();
            return Results.Content(RenderingService.RenderAlerts(db), "text/html");
        });
    }
}
