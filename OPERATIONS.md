# Blink Notifier — Operations

## Deployment

### Portable EXE

Unzip the `BlinkNotifier-x.y.z-win-x64.zip` release artifact. Run `BlinkNotifier.App.exe` directly — no installation, no admin rights. The app creates its data directories under `%LOCALAPPDATA%\BlinkNotifier\` on first run.

### MSIX

Double-click `BlinkNotifier-x.y.z.msix`. Windows installs the package under `%ProgramFiles%\WindowsApps\`. The manifest includes a `windows.startupTask` extension so the app can appear in Windows startup settings.

For Store distribution, upload the MSIX through Microsoft Partner Center. The app must be re-signed with a Partner Center–trusted certificate before Store submission; the self-signed certificate in CI is for smoke testing only.

### Microsoft Store submission checklist

**Prerequisites**

- [ ] Microsoft Partner Center account enrolled in the Windows developer program (one-time $19 USD fee)
- [ ] Code-signing certificate trusted by Partner Center (EV cert from DigiCert / Sectigo, or use Partner Center's own managed signing)
- [ ] GitHub Pages enabled and `docs/privacy.html` accessible at its public URL (repo Settings → Pages → Source: GitHub Actions)

**Package preparation**

- [ ] Replace placeholder icons in `src/BlinkNotifier.Packaging/Images/` with real artwork:
  - `Square44x44Logo.png` — 44×44 px (also needed at 55×55, 66×66, 88×88, 176×176 — use scale folders or a single source)
  - `Square150x150Logo.png` — 150×150 px
  - `Wide310x150Logo.png` — 310×150 px
  - `StoreLogo.png` — 50×50 px
  - `SplashScreen.png` — 620×300 px
- [ ] Update `Package.appxmanifest` `Publisher` field to match the certificate's Subject DN exactly
- [ ] Trigger `release.yml` via `git tag v1.0.0 && git push --tags` — produces a draft GitHub Release with a signed MSIX
- [ ] Download the MSIX from the draft release and smoke-test installation on a clean Windows machine

**Store listing (Partner Center → Apps → New product)**

- [ ] Reserve the app name "Blink Notifier" (or localised variant)
- [ ] Category: **Productivity** → Utilities & tools
- [ ] Age rating: complete the IARC questionnaire (expected result: **3+** / Everyone — no violence, no in-app purchases, no personal data collection)
- [ ] Privacy policy URL: the GitHub Pages URL for `docs/privacy.html`
- [ ] Support contact: your email or a GitHub Issues URL
- [ ] Short description (≤ 200 chars): _"A lightweight system-tray reminder to follow the 20-20-20 eye-care rule. Configurable interval, schedule, and fullscreen auto-pause."_
- [ ] Long description (≤ 10 000 chars): expand from README "Why" + "Jobs it does" sections
- [ ] Keywords: `eye strain`, `20-20-20`, `eye care`, `productivity`, `reminder`, `screen break`
- [ ] Screenshots: at least 1 desktop screenshot (1366×768 minimum); recommended — tray icon + context menu, toast notification, Settings window
- [ ] Pricing: **Free**, no in-app purchases

**Submission**

- [ ] Upload the signed MSIX produced by `release.yml`
- [ ] Submit for certification (typically 1–3 business days)
- [ ] After approval: publish the GitHub Release draft and update `CHANGELOG.md` with the actual release date

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
