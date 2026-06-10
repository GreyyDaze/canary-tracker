using CanaryTracker.Data;
using CanaryTracker.Models;

namespace CanaryTracker.Services;

public static class RenderingService
{
    public static string Icon(string name, int size = 14) => name switch
    {
        "shield" => $"<svg xmlns='http://www.w3.org/2000/svg' width='{size}' height='{size}' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M12 22s-8-4.5-8-11.8A8 8 0 0 1 12 2a8 8 0 0 1 8 8.2c0 7.3-8 11.8-8 11.8z'/></svg>",
        "alert-circle" => $"<svg xmlns='http://www.w3.org/2000/svg' width='{size}' height='{size}' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><circle cx='12' cy='12' r='10'/><line x1='12' y1='8' x2='12' y2='12'/><line x1='12' y1='16' x2='12.01' y2='16'/></svg>",
        "bar-chart" => $"<svg xmlns='http://www.w3.org/2000/svg' width='{size}' height='{size}' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M12 20V10'/><path d='M18 20V6'/><path d='M6 20v-4'/></svg>",
        "activity" => $"<svg xmlns='http://www.w3.org/2000/svg' width='{size}' height='{size}' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><polyline points='22 12 18 12 15 21 9 3 6 12 2 12'/></svg>",
        "key" => $"<svg xmlns='http://www.w3.org/2000/svg' width='{size}' height='{size}' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M2.586 17.414A2 2 0 0 0 2 18.828V21a1 1 0 0 0 1 1h3a1 1 0 0 0 1-1v-1a1 1 0 0 1 1-1h1a1 1 0 0 0 1-1v-1a1 1 0 0 1 1-1h.172a2 2 0 0 0 1.414-.586l.814-.814a6.5 6.5 0 1 0-4-4z'/><circle cx='16.5' cy='7.5' r='.5' fill='currentColor'/></svg>",
        "wifi" => $"<svg xmlns='http://www.w3.org/2000/svg' width='{size}' height='{size}' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M5 12.55a11 11 0 0 1 14.08 0'/><path d='M1.42 9a16 16 0 0 1 21.16 0'/><path d='M8.53 16.11a6 6 0 0 1 6.95 0'/><circle cx='12' cy='20' r='1'/></svg>",
        "eye" => $"<svg xmlns='http://www.w3.org/2000/svg' width='{size}' height='{size}' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z'/><circle cx='12' cy='12' r='3'/></svg>",
        "triangle" => $"<svg xmlns='http://www.w3.org/2000/svg' width='{size}' height='{size}' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z'/><line x1='12' y1='9' x2='12' y2='13'/><line x1='12' y1='17' x2='12.01' y2='17'/></svg>",
        "check" => $"<svg xmlns='http://www.w3.org/2000/svg' width='{size}' height='{size}' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><polyline points='20 6 9 17 4 12'/></svg>",
        "thumbs-up" => $"<svg xmlns='http://www.w3.org/2000/svg' width='{size}' height='{size}' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M7 10v12'/><path d='M15 5.88 14 10h5.83a2 2 0 0 1 1.92 2.56l-2.33 8A2 2 0 0 1 17.5 22H4a2 2 0 0 1-2-2v-8a2 2 0 0 1 2-2h2.76a2 2 0 0 0 1.79-1.11L12 2h0a3.13 3.13 0 0 1 3 3.88Z'/></svg>",
        "folder" => $"<svg xmlns='http://www.w3.org/2000/svg' width='{size}' height='{size}' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z'/></svg>",
        _ => ""
    };

    public static string RenderCanarySection(AppDbContext db)
    {
        var canaries = db.Canaries.OrderByDescending(c => c.CreatedAt).ToList();
        var html = $"<div class='panel'><h2>{Icon("shield")} Canaries</h2><table>";
        html += "<tr><th>Name</th><th>Category</th><th>Location</th><th>Status</th></tr>";
        foreach (var c in canaries)
        {
            var cls = c.Status == "active" ? "badge-active" : "badge-compromised";
            var icn = c.Status == "active" ? Icon("eye") : Icon("alert-circle");
            var fileName = c.Category switch
            {
                CanaryCategory.Cloud => "credentials",
                CanaryCategory.Infrastructure => "id_rsa",
                CanaryCategory.Credential => "passwords.txt",
                _ => ""
            };
            var path = "~/.config/canary/" + Path.GetFileName(c.DirectoryPath) + "/" + fileName;
            html += $"<tr><td><strong>{c.Name}</strong></td>";
            html += $"<td>{c.Category}</td>";
            html += $"<td><code>{path}</code></td>";
            html += $"<td><span class='badge {cls}'>{icn} {c.Status}</span></td></tr>";
        }
        if (canaries.Count == 0)
            html += $"<tr><td colspan='4'><div class='empty-state'>{Icon("key")} No canaries deployed yet.</div></td></tr>";
        html += "</table></div>";
        return html;
    }

    public static string RenderStats(AppDbContext db)
    {
        var total = Enum.GetValues<CanaryCategory>().Length;
        var covered = db.Canaries.Select(c => c.Category).Distinct().Count();
        var coverage = total > 0 ? 100 * covered / total : 0;
        var compromised = db.Canaries.Count(c => c.Status == "compromised");
        var totalAlerts = db.Alerts.Count();

        var html = "<div class='stats'>";
        html += $"<div class='stat-card'>{Icon("shield")}<div class='label'>Coverage</div><div class='value'>{coverage}%</div><div class='sub'>{covered}/{total} categories</div></div>";
        html += $"<div class='stat-card'>{Icon("eye")}<div class='label'>Deployed</div><div class='value'>{db.Canaries.Count()}</div><div class='sub'>canaries</div></div>";
        html += $"<div class='stat-card'>{Icon("alert-circle")}<div class='label'>Compromised</div><div class='value {(compromised > 0 ? "red" : "green")}'>{compromised}</div><div class='sub'>canaries</div></div>";
        html += $"<div class='stat-card'>{Icon("activity")}<div class='label'>Total Events</div><div class='value'>{totalAlerts}</div><div class='sub'>detected</div></div>";
        html += "</div>";
        return html;
    }

    public static string RenderAlerts(AppDbContext db)
    {
        var alerts = db.Alerts.OrderByDescending(a => a.DetectedAt).Take(50).ToList();
        var html = $"<div class='panel'><h2>{Icon("activity")} Detection Events</h2>";

        foreach (var a in alerts)
        {
            var cls = a.Status == AlertStatus.New ? "alert-new" : "";
            var icon = a.Status == AlertStatus.New ? Icon("alert-circle") : Icon("check");
            html += $"<div class='alert-item {cls}'>";
            html += $"{icon}";
            html += $"<div class='alert-body'>";
            html += $"<div class='alert-msg'>{a.Message}</div>";
            html += $"<div class='alert-meta'>canary: <strong>{a.CanaryName}</strong> &middot; {a.Source} &middot; {a.DetectedAt:HH:mm:ss}</div>";
            html += "</div>";
            if (a.Status == AlertStatus.New)
                html += $"<button class='btn btn-small btn-ack' hx-post='/alerts/{a.Id}/acknowledge' hx-target='#alert-section'>Ack</button>";
            else if (a.Status == AlertStatus.Acknowledged)
                html += $"<button class='btn btn-small btn-resolve' hx-post='/alerts/{a.Id}/resolve' hx-target='#alert-section'>Resolve</button>";
            html += "</div>";
        }

        if (alerts.Count == 0)
            html += $"<div class='empty-state'>{Icon("check")} No events detected yet. Deploy a canary and try modifying the file.</div>";

        html += "</div>";
        return html;
    }
}
