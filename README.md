# Watchdog
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A generic, configurable monitoring and notification service for Windows.

## The Origin Story

If you mountain bike in the Atlanta area, you know the drill. It rained last night. You wake up, you *think* the trails might be dry enough, but you have no idea. So you pull up the [SORBA Woodstock trail status page](https://www.sorbawoodstock.org/trail-status/), and it says closed. Okay, fine. You check again an hour later. Still closed. You go make coffee. You check again. You refresh. Still closed. You refresh again just in case. Still closed. This goes on all morning.

Watchdog exists so you don't have to do that. Instead of you checking the website, Watchdog checks it for you every 15 minutes and sends you a text the moment the status changes. Put your phone down. Go do something else. You'll know when the trails are open.

Once it was built, it was easy to make it generic — so now it can monitor pretty much anything on the web and notify you when something changes.

## What It Does

Watchdog runs as a Windows Service and periodically checks any number of configured monitors. When a monitor's value changes, it sends a notification via the configured channel(s). State is persisted across restarts so you won't get spammed with redundant notifications if the service or machine restarts.

### Monitor Types

| Type | What it does |
|---|---|
| `WebContent` | Fetches a page and extracts text using a regex pattern. Notifies when the extracted text changes. |
| `HttpStatus` | Checks that a URL returns an expected HTTP status code. Notifies when the status changes. |
| `KeywordPresence` | Checks whether a keyword is present or absent on a page. Notifies when that changes. |

### Notification Channels

| Type | What it does |
|---|---|
| `Smtp` | Sends email via SMTP. Works with Gmail App Passwords. (Carrier email-to-SMS gateways such as Verizon's vtext.com have been shut down; use `Ntfy` for phone alerts.) |
| `Ntfy` | Sends a push notification through [ntfy](https://ntfy.sh), either the public ntfy.sh or a self-hosted server. Supports access tokens (preferably from a file), priority, tags/emoji and a tap-to-open URL. |

## Setup

### 1. Configure

Copy `appsettings.Sample.json` to `appsettings.json` and fill in your details:

```jsonc
{
  "NotificationMonitor": {
    "Channels": [
      {
        "Id": "gmail-smtp",
        "Type": "Smtp",
        "Smtp": {
          "Username": "you@gmail.com",
          "Password": "your-gmail-app-password",  // Generate at myaccount.google.com → Security → App passwords
          "FromAddress": "you@gmail.com",
          "Recipients": ["you@example.com"]
        }
      },
      {
        "Id": "phone-ntfy",
        "Type": "Ntfy",
        "Ntfy": {
          "ServerUrl": "https://ntfy.sh",          // or your self-hosted server
          "Topic": "your-topic",
          "AccessTokenFile": "C:\\path\\to\\token", // file containing only the tk_... token (or use "AccessToken")
          "Priority": 4,                           // 1–5
          "Tags": ["bicyclist"]                    // emoji shortcodes show as emoji
        }
      }
    ],
    "Monitors": [
      // add your monitors here — see appsettings.Sample.json for examples
    ]
  }
}
```

### 2. Build

```bash
dotnet build -c Release
```

### 3. Install as a Windows Service

From an elevated command prompt:

```bash
sc create Watchdog binPath="C:\path\to\Watchdog.exe" start= auto
sc start Watchdog
```

The service will start automatically on reboot and send a startup notification for any monitor with `"NotifyOnStart": true`.

### 4. Uninstall

```bash
sc stop Watchdog
sc delete Watchdog
```

## Adding a New Monitor

Just add an entry to the `Monitors` array in `appsettings.json` — no code changes needed. Pick a unique `Id`, set the `Type`, point it at a channel, and you're done.

```json
{
  "Id": "my-new-monitor",
  "DisplayName": "My New Monitor",
  "Type": "HttpStatus",
  "IntervalSeconds": 300,
  "NotifyOnStart": false,
  "ChannelIds": ["gmail-smtp"],
  "HttpStatus": {
    "Url": "https://example.com/",
    "ExpectedStatusCode": 200
  }
}
```
