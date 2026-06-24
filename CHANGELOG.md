# Changelog

All notable changes to Blink Notifier are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [Unreleased]

### Fixed

- Schedule settings could be saved with no active days selected when checkboxes were changed
  while the schedule was already enabled (day-setter validation was not re-triggered).

## [1.0.0] — planned

### Added

- Timer-based 20-20-20 eye-break reminders via Windows toast notifications.
- Toast action buttons: Snooze 5 min, Snooze 15 min, Snooze 60 min, Dismiss.
  - Dismiss restarts the full interval from now.
  - Snooze fires exactly when the snooze duration expires.
- Configurable reminder interval (1–60 minutes, default 20 min), persisted across restarts.
- System tray icon with right-click menu (Start / Stop / Settings / Exit).
- Settings window: interval slider, schedule controls (active hours + day-of-week), auto-launch toggle.
- Schedule-based suppression — notifications only fire within a configured daily time window and day selection.
- Fullscreen auto-pause — detects exclusive and windowed-fullscreen windows and suppresses the notification; resumes automatically.
- Windows startup auto-launch via registry entry (toggle in Settings or first-run wizard).
- First-run wizard confirms startup preference on initial launch.
- Single-instance enforcement via global named Mutex.
- Fully offline — zero network calls, no telemetry, no account required.
- Settings stored at `%LOCALAPPDATA%\BlinkNotifier\settings.json`.
- Rolling structured log at `%LOCALAPPDATA%\BlinkNotifier\logs\` (7-day retention).
- Portable self-contained EXE (no installer, no admin rights) and MSIX installer in GitHub Releases.
- MIT license.
