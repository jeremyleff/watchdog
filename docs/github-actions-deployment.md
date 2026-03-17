# GitHub Actions Deployment

This document outlines the plan for deploying Watchdog as a GitHub Actions scheduled workflow, as an alternative to running it as a local Windows Service.

## Overview

Instead of a long-running Windows Service, a GitHub Actions workflow runs Watchdog on a cron schedule. Each run checks all configured monitors once and exits. GitHub's infrastructure handles the scheduling — no local machine required.

## Required Code Change: One-Shot Mode

The current `Program.cs` starts an `IHostedService` that loops forever via `PeriodicTimer`. GitHub Actions needs a mode that runs each monitor once and exits.

Add a `--mode once` argument to `Program.cs`:

```csharp
if (args.Contains("--mode=once"))
{
    // Build host without UseWindowsService()
    // Resolve MonitorOrchestrator, call RunOnceAsync(), exit
}
else
{
    // Existing Windows Service path
    Host.CreateDefaultBuilder(args)
        .UseWindowsService()
        ...
}
```

`MonitorOrchestrator` would need a `RunOnceAsync()` method that iterates all monitors once — the check/notify/persist-state logic is identical to what already runs inside the timer loop.

## State Persistence

Watchdog uses local files in `state/` to track last-known status and avoid re-notifying on every run. In GitHub Actions, the filesystem is ephemeral — state is lost between runs.

Options (pick one):

| Option | Complexity | Cost |
|---|---|---|
| Commit `state/` back to the repo after each run | Low | Free |
| Store state in a GitHub Gist | Low | Free |
| Azure Blob Storage | Medium | Free tier (LRS, 5GB) |
| Supabase (Postgres) | Medium | Free tier |

**Recommended:** Commit state back to the repo. Simple, no external dependencies, and the history is a useful audit log.

## GitHub Actions Workflow

Create `.github/workflows/watchdog.yml`:

```yaml
name: Watchdog

on:
  schedule:
    - cron: '*/15 * * * *'  # Every 15 minutes (minimum GitHub Actions interval is 5 min)
  workflow_dispatch:          # Allow manual runs

jobs:
  run:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Run monitors
        env:
          NotificationMonitor__Channels__0__Smtp__Username: ${{ secrets.SMTP_USERNAME }}
          NotificationMonitor__Channels__0__Smtp__Password: ${{ secrets.SMTP_PASSWORD }}
          NotificationMonitor__Channels__0__Smtp__FromAddress: ${{ secrets.SMTP_FROM }}
        run: dotnet run --project Watchdog/Watchdog.csproj -- --mode=once

      - name: Commit state
        run: |
          git config user.name "github-actions[bot]"
          git config user.email "github-actions[bot]@users.noreply.github.com"
          git add state/
          git diff --staged --quiet || git commit -m "chore: update monitor state"
          git push
```

## GitHub Secrets

Add these in **Settings → Secrets and variables → Actions**:

| Secret | Value |
|---|---|
| `SMTP_USERNAME` | Gmail address |
| `SMTP_PASSWORD` | Gmail App Password |
| `SMTP_FROM` | Gmail address (same as username) |

## Caveats

- GitHub Actions scheduled jobs can be delayed by several minutes under load — not suitable for sub-minute alerting.
- Scheduled workflows on public repos may be disabled if the repo has no activity for 60 days. A periodic manual run or a small commit resets this.
- The minimum cron interval is 5 minutes.
