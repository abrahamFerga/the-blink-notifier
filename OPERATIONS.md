# Blink Notifier — Operations

## Deployment

### Portable EXE

Unzip the `BlinkNotifier-x.y.z-win-x64.zip` release artifact. Run `BlinkNotifier.App.exe` directly — no installation, no admin rights. The app creates its data directories under `%LOCALAPPDATA%\BlinkNotifier\` on first run.

### MSIX

Double-click `BlinkNotifier-x.y.z.msix`. Windows installs the package under `%ProgramFiles%\WindowsApps\`. The manifest includes a `windows.startupTask` extension so the app can appear in Windows startup settings.

For Store distribution, upload the MSIX through Microsoft Partner Center. The app must be re-signed with a Partner Center–trusted certificate before Store submission; the self-signed certificate in CI is for smoke testing only.

### GitHub Actions CI/CD

| Workflow | Trigger | Output |
|---|---|---|
| `ci.yml` | Push / PR to `main` or `feat/**`; manual dispatch | Build + test |
| `release.yml` | Git tag `v*` | Draft GitHub Release with portable ZIP + MSIX |
| `pages.yml` | Push to `main` (docs/ changed) | GitHub Pages deployment |

To cut a release:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

The release workflow builds, signs (self-signed in CI), and creates a draft release. Review the draft, update the release notes, and publish.

## Data locations

| Path | Contents | Retention |
|---|---|---|
| `%LOCALAPPDATA%\BlinkNotifier\settings.json` | User preferences | Until user deletes or uninstalls |
| `%LOCALAPPDATA%\BlinkNotifier\logs\blink-YYYYMMDD.json` | Structured Serilog log | 7 rolling days (auto-deleted) |

No data is stored elsewhere. No network calls are made.

## First run

On first launch without `--startup`, the First Run Wizard appears and asks the user to confirm or disable auto-launch. Subsequent launches with `--startup` (passed by the Windows startup entry) skip the wizard.

## GDPR procedures

**Article 20 — Data portability:** There is no personal data to export. All state is the user's own settings file at the path above, which they can copy at any time.

**Article 17 — Right to erasure:** Delete `%LOCALAPPDATA%\BlinkNotifier\` and uninstall the app (via Apps & Features for MSIX, or delete the EXE for portable). No residual data remains.

No data processing agreement, consent banner, or data-subject-rights API is required because the app collects no personal data.

## Incident playbook

### App won't start

1. Check Event Viewer → Windows Logs → Application for entries from source `Blink Notifier`.
2. Check `%LOCALAPPDATA%\BlinkNotifier\logs\` for `[Error]` or `[Fatal]` entries.
3. If `settings.json` is corrupt: delete it. The app recreates defaults on next launch.
4. Confirm the machine is Windows 10 1809+ x64.

### Notifications not appearing

1. Check Windows Settings → Notifications → Blink Notifier — confirm notifications are allowed.
2. Check that Focus Assist / Do Not Disturb is not blocking notifications.
3. Look for `"Snooze active"` or `"Outside schedule window"` in the log — the timer may be paused legitimately.
4. Check for `"Fullscreen active"` in the log — a fullscreen window suppresses toasts.

### Settings window won't open

The Settings window is a WPF dialog launched from the tray icon. If it fails:
1. Look for a WPF dispatcher exception in the Serilog log.
2. Try right-clicking the tray icon and selecting **Exit**, then restarting the app.

### Duplicate tray icons / multiple instances

The app uses a global named Mutex (`Global\BlinkNotifier-SingleInstance`) to enforce single-instance. If two icons appear, the second instance should exit immediately after failing to acquire the mutex. If this doesn't happen, check for a stale mutex from a crashed process by restarting the machine.
