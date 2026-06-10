# Canary Tracker — Architecture Guide

This document explains every file, the data flow, the API routes, and the tricky parts of the code so you understand the project completely before the interview.

---

## Project Structure

```
canary-tracker/
├── Program.cs                  # Main app: routes, HTML rendering, startup
├── Models/
│   ├── Canary.cs               # Canary data model
│   └── Alert.cs                # Alert data model
├── Data/
│   └── AppDbContext.cs         # Database setup (EF Core + SQLite)
├── Services/
│   └── CanaryWatcherService.cs # Background file system monitoring
├── wwwroot/
│   └── index.html              # Dashboard UI (HTMX)
└── docs/
    └── architecture.md         # This file
```

---

## How It Works — Complete Flow

### Step-by-step walkthrough

#### 1. Starting the app

```
dotnet run
```

`Program.cs` runs this sequence:

1. **Line 6-7**: Gets your home directory and sets `BaseDir` to `~/.config/canary/`
2. **Line 15**: Registers SQLite database via Entity Framework Core
3. **Line 16**: Registers `CanaryWatcherService` as a background service (starts immediately)
4. **Line 26-27**: Serves static files from `wwwroot/` (the HTML dashboard)
5. **Line 196**: Starts the web server on http://localhost:5193

The background service (`CanaryWatcherService.ExecuteAsync`) starts a `FileSystemWatcher` on `~/.config/canary/` that watches for file changes, deletions, and renames.

#### 2. User clicks "Deploy AWS Key"

The HTML form sends a POST request via HTMX:

```html
<form hx-post="/canaries" hx-target="#canary-section" style="display:inline">
    <input type="hidden" name="name" value="aws-prod-key">
    <input type="hidden" name="category" value="Cloud">
    <button type="submit">Deploy AWS Key</button>
</form>
```

`hx-post="/canaries"` sends the form data to the server.
`hx-target="#canary-section"` puts the server's response HTML inside the `<div id="canary-section">`.

The server handler (line 125-173):

1. **Reads the form**: `await ctx.Request.ReadFormAsync()` — gets `name` and `category`
2. **Creates a directory**: `/Users/apple/.config/canary/aws-prod-key-20260610-101645/`
3. **Calls `MarkDeploy(dir)`**: Tells the watcher "this is our own file, don't alert on it" (prevents false positive)
4. **Writes a credential file**: Based on category — `credentials` (AWS format), `id_rsa` (SSH format), or `passwords.txt`
5. **Saves to database**: Creates a `Canary` record
6. **Returns HTML**: `RenderCanarySection(db)` renders the canary table and HTMX swaps it into the page

#### 3. An attacker modifies the file

```bash
echo "stolen_data=true" >> ~/.config/canary/aws-prod-key-*/credentials
```

This triggers:
1. **FileSystemWatcher.Changed** event fires
2. `OnFileEvent` runs with `action = "modified"` and `fullPath = ".../credentials"`
3. It checks: is this within 1 second of our own deploy? If yes → ignore.
4. It checks: is this a duplicate event within 5 seconds? If yes → ignore.
5. It waits 800ms (lets the file operation finish)
6. It runs `lsof` on the file to see what process has it open
7. It creates an `Alert` record in the database

#### 4. The dashboard shows the alert

The `<div id="alert-section">` has `hx-trigger="load, every 3s"` — HTMX polls the server every 3 seconds:

```
GET /alerts → returns HTML with all alerts → swapped into #alert-section
```

The alert shows:
- Message: "File modified — possible tampering by unknown process"
- Source: "modified (apple)"
- Timestamp

---

## File-by-File Explanation

### `Models/Canary.cs`

```csharp
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
```

Simple database model. `DirectoryPath` stores the full path like `/Users/apple/.config/canary/aws-prod-key-20260610-101645/`. This is how the watcher matches file events to canaries.

`required` keyword means the property must be set when creating the object (compile-time check, no nulls).

### `Models/Alert.cs`

```csharp
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
```

`Source` stores either `"modified (apple)"` (when lsof doesn't catch the process) or `"zsh (PID 12345, user: apple)"` (when it does).

### `Data/AppDbContext.cs`

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Canary> Canaries => Set<Canary>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=canary.db");

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Canary>().HasIndex(c => c.Name);
        model.Entity<Alert>().HasIndex(a => a.DetectedAt);
    }
}
```

- `UseSqlite` stores the database in `canary.db` (a local file, no server needed)
- `HasIndex` creates database indexes for faster lookups by Name and DetectedAt

### `Services/CanaryWatcherService.cs` (the hardest part)

**Two tricks that avoid false alerts:**

1. **`_deployDirs` dictionary** (deploy suppression):
   - When we deploy a canary, `MarkDeploy(dir)` records the directory path + current time
   - When `OnFileEvent` fires, it checks if this directory was just deployed (< 1 second ago)
   - If yes → ignores the event (it's our own file write)
   - This prevents every deploy from triggering a false "tampering" alert

2. **`_recentEvents` dictionary** (dedup):
   - macOS `FileSystemWatcher` often fires multiple events for a single operation (one for size change, one for metadata)
   - The dictionary tracks `{filePath}:{action}` → last event time
   - If the same event fired within 5 seconds → ignores it
   - This prevents duplicate alerts for a single `echo >>` command

**`GetProcessInfo` method (line 87-111):**

```csharp
private static async Task<string?> GetProcessInfo(string filePath)
{
    var psi = new ProcessStartInfo("lsof", filePath)
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    using var proc = Process.Start(psi);
    var output = await proc.StandardOutput.ReadToEndAsync();
    // Parse output to find command, PID, and username
}
```

- Runs the macOS `lsof` command which lists processes with open file handles
- Parses the output to extract: command name, PID, username
- Example: `"zsh (PID 12345, user: apple)"`

**Limitation:** If the process opens, writes, and closes the file faster than 800ms (like `echo >>`), lsof won't find it. It catches longer-lived access like `vim`, `less`, or Finder file operations.

### `Program.cs`

**Three rendering functions (line 45-117):**

Each returns an HTML string that HTMX swaps into the page.

- `RenderCanarySection(db)` — table of deployed canaries with status badges
- `RenderStats(db)` — 4 stat cards (coverage %, deployed count, compromised count, total events)
- `RenderAlerts(db)` — list of detection events with Ack/Resolve buttons

**API routes (line 119-196):**

```
GET  /stats                  → RenderStats HTML
GET  /canaries               → RenderCanarySection HTML
POST /canaries               → deploy a canary, write file, return updated canary section
GET  /alerts                 → RenderAlerts HTML
POST /alerts/{id}/acknowledge → mark alert acknowledged, return updated alerts
POST /alerts/{id}/resolve     → mark alert resolved, return updated alerts
```

**The `Icon()` function (line 29-43):**

A switch expression that returns inline SVG strings for each icon name. Used throughout all the HTML rendering functions. Avoids having to copy-paste SVG markup everywhere.

### `wwwroot/index.html`

A single-page HTML file using HTMX loaded from CDN:

```html
<script src="https://unpkg.com/htmx.org@2.0.0"></script>
```

**Three HTMX polling sections:**

```html
<div id="stats-bar"   hx-get="/stats"    hx-trigger="load, every 5s"></div>
<div id="canary-section" hx-get="/canaries" hx-trigger="load, every 5s"></div>
<div id="alert-section"  hx-get="/alerts"   hx-trigger="load, every 3s"></div>
```

`hx-trigger="load, every 5s"` means: fetch immediately on page load, then every 5 seconds.

**Forms use HTMX for submission:**

```html
<form hx-post="/canaries" hx-target="#canary-section">
```

The form sends POST to /canaries and the response HTML replaces the content of `#canary-section` without a full page refresh.

---

## Common Questions

**Q: Why can't we detect `cat file` (read-only access)?**

macOS APFS uses `noatime` — access times are not updated. FileSystemWatcher (which uses `kqueue` on macOS) only detects: writes, deletes, renames. Reads are invisible at the OS level without kernel extensions (requires root or System Extension approval).

**This is the same reason Tracebit detects credential USAGE (API calls), not file reads.** The file is just the delivery mechanism.

**Q: How does HTMX polling work?**

When the page loads, HTMX scans elements with `hx-get` + `hx-trigger`. For `hx-trigger="load, every 5s"`, it:
1. Sends GET request immediately on page load
2. Puts the response HTML inside the element (innerHTML swap)
3. Starts a 5-second timer
4. Every 5 seconds, sends another GET request and swaps the response

**Q: Why use inline SVG icons instead of an icon library?**

Lucide was removed because loading from CDN added network dependency and the `lucide.createIcons()` re-scan was causing issues after HTMX swaps. Inline SVGs are permanent, zero-dependency, and always show correctly.

**Q: Why `DateTime.UtcNow` instead of `DateTime.Now`?**

UTC times are timezone-independent. When displaying, we format with `:HH:mm:ss` which shows the server's local time. Using UTC avoids bugs when the app runs across timezone boundaries or during daylight saving changes.

**Q: Why does the Acknowledge/Resolve endpoint return full HTML instead of empty string?**

Previously, acknowledge returned `Results.Content("", "text/html")` which wiped the alert section content. Even though polling would refill it after 3 seconds, the user saw a blank section in the meantime. Returning the full alert HTML gives immediate feedback.
