# Blink Notifier

A zero-friction Windows system-tray app that fires a gentle, dismissible 20-20-20 toast notification on a configurable timer, so screen-bound knowledge workers never forget to blink without disrupting their flow.

## Why

Staring at a screen without blinking dries out your eyes. The 20-20-20 rule (every 20 minutes, look at something 20 feet away for 20 seconds) helps — but only if you remember to do it. Blink Notifier handles the remembering.

## Jobs it does

- Fires a Windows toast notification at your chosen interval (1–60 min, default 20 min).
- Snooze in one click (5 min / 15 min / 60 min options), or dismiss to restart the timer from now.
- Suppresses notifications outside a configured daily time window and day-of-week selection.
- Pauses automatically while a fullscreen window is active (games, video, browser video).
- Starts with Windows automatically — set once, never think about it again.
- Stores all settings locally. Zero network calls. No account. No telemetry.

## Getting started

### Download (recommended)

From [GitHub Releases](https://github.com/abrahamFerga/the-blink-notifier/releases):

| Artifact | When to use |
|---|---|
| `BlinkNotifier-x.y.z-win-x64.zip` | Unzip and run — no installer, no admin rights required |
| `BlinkNotifier-x.y.z.msix` | Double-click to install; registers in Apps & Features |

The MSIX requires a trusted certificate. The portable ZIP runs on any Windows 10 1809+ (x64) machine.

### Build from source

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) on Windows 10 or 11 (x64).

```powershell
git clone https://github.com/abrahamFerga/the-blink-notifier.git
cd the-blink-notifier
dotnet build --configuration Release
dotnet test  --configuration Release --no-build
```

Run the app:

```powershell
dotnet run --project src/BlinkNotifier.App/BlinkNotifier.App.csproj
```

Publish a self-contained single-file EXE:

```powershell
dotnet publish src/BlinkNotifier.App/BlinkNotifier.App.csproj `
  --configuration Release --runtime win-x64 `
  --self-contained true -p:PublishSingleFile=true `
  --output publish/
```

## Settings

Right-click the tray icon → **Settings**.

| Setting | Default | Notes |
|---|---|---|
| Reminder interval | 20 min | 1–60 min |
| Auto-launch at startup | On | Adds a Windows startup entry |
| Schedule | Off | Set active hours and days |

Settings are saved to `%LOCALAPPDATA%\BlinkNotifier\settings.json`.

## Privacy

No data leaves your device. See [docs/privacy.html](docs/privacy.html) or the hosted policy at the GitHub Pages URL for this repo.

## Contributing

Open issues and pull requests at [github.com/abrahamFerga/the-blink-notifier](https://github.com/abrahamFerga/the-blink-notifier). Bug reports are especially welcome.

## License

MIT — see [LICENSE](LICENSE).
