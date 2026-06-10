# Canary Tracker

A small prototype exploring deploy → detect → alert for honeypot credentials on macOS.

---

## Why I Built This

I wanted to understand canary-based detection by building one — get hands-on with the deploy → detect → alert loop and see where the hard parts are.

Three things surprised me:

1. File reads are invisible on macOS — no OS exposes them. Detection has to happen at the _usage_ level, not the file level.
2. Suppressing your own deploy events is harder than it sounds — one wrong filter and every deployment looks like an attack.
3. Process enrichment via `lsof` is fragile — fast commands finish before you can query them.

---

## What It Does

- **Deploy** — writes a real credential file (AWS key, SSH key, passwords) to `~/.config/canary/`
- **Detect** — `FileSystemWatcher` monitors for modifications, deletions, renames
- **Enrich** — `lsof` captures the process name, PID, and user when possible
- **Alert** — live-updating dashboard via HTMX polling (no JavaScript framework)

> Intentionally scoped to the local deploy → detect → alert loop. The real challenge is what comes after — using AI/ML to learn behavioral patterns, deploying remotely across a fleet, and distinguishing real attacker behavior from noise at scale. This prototype helped me understand the foundational layer everything else sits on top of.

---

## How It Works

```
Deploy → write file to disk → FileSystemWatcher starts monitoring
                              ↓
Attacker modifies/deletes file → event fires → suppress if self-inflicted
                              ↓
                    lsof checks for process info
                              ↓
                    Alert created → HTMX poll shows it in 3s
```

**Tech stack:** .NET 10 · SQLite (EF Core) · HTMX 2.0

---

## Demo

> _(10-15 second GIF: click Deploy AWS Key → echo a line to the file in terminal → alert appears on dashboard)_

---

## Quick Start

```bash
cd ~/Desktop/projects/canary-tracker
dotnet run
```

Open http://localhost:5193. Click **Deploy AWS Key**, then:

```bash
echo "stolen" >> ~/.config/canary/aws-prod-key-*/credentials
```

Alert appears within 3 seconds.

---

## What I Learned

- macOS doesn't expose file reads — detection has to happen at the infrastructure layer (API calls, SSH auth, etc.)
- Deploy suppression requires tracking state per canary, not globally
- HTMX polling is surprisingly elegant for live dashboards — no WebSocket, no JS framework

If I had more time:

- Add more canary types (browser cookies, password manager entries, email trackers not just file credentials
- Add AI-generated canary content that looks convincingly real per environment
- Push deploy/detect/alert into a lightweight CLI so you don't need the browser at all
- Add a lightweight agent that deploys canaries across multiple machines and reports alerts back to a central dashboard
- Build behavioral baselines per canary — learn when files are normally accessed (cron jobs, backups) and suppress those automatically
- Add severity scoring so a `cat` during business hours scores lower than an `echo` at 3am from an unknown process
- Test against real tooling (Mimikatz, LaZagne) to see what trips detection and what slips through on macOS vs Linux
- Add Slack/webhook alert delivery — the dashboard is nice but real alerting goes where the team already works
- Package as a launchd service so it survives reboots and runs headless on a laptop or server

---

## About Me

I'm a builder exploring detection infrastructure. This prototype covers the local foundation — I know the hard problems live in the AI layer, remote deployment, and scale.

I'd love to hear what I got wrong.

[LinkedIn] · [Email]
