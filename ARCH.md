# The Blink Notifier — Architecture

## Context (C4 L1)

The Blink Notifier is a standalone Windows desktop application with no server, no network stack,
and no external runtime dependencies beyond the Windows platform APIs. All integrations are with
the local OS.

**Actors:**
- **Windows User** — the person running the app (Desk Developer, Office Professional, Chronic Dry Eye Sufferer personas from SPEC)

**External systems at runtime:**
- **Windows Notification System** — renders toast notifications and routes Snooze/Dismiss action-button callbacks back to the app via COM activation
- **Windows Registry** — stores the startup task registration and (for the portable EXE) the AUMID COM server entry

**External systems at distribution time only (never contacted at runtime):**
- **GitHub Releases** — hosts the portable EXE .zip artifact
- **Microsoft Store** — hosts the MSIX-packaged artifact

Diagram: `docs/diagrams/c1-context.puml`

---

## Containers (C4 L2)

All containers are in-process components of a single `.exe` process. There is no separate
service, daemon, or API server.

| Container | Type | Technology | Purpose |
|---|---|---|---|
| `BlinkNotifier.App` | WPF application project | WPF .NET 10, Generic Host | Entry point; single-instance enforcement; system tray icon; first-run wizard; settings window shell |
| `BlinkNotifier.Core` | Class library | .NET 10, WinRT interop | Timer engine, toast dispatch, fullscreen polling, snooze state machine, COM activation callback handler |
| `BlinkNotifier.Settings` | Class library | .NET 10, System.Text.Json | Settings model (`BlinkSettings`), `IOptions<T>` validation, JSON persistence to `%LOCALAPPDATA%` |
| `BlinkSettings.json` | Persistent store | JSON file at `%LOCALAPPDATA%\BlinkNotifier\settings.json` | All app state: interval, schedule, snooze options, auto-launch flag, schema version |
| `Windows Notification System` | External system | WinRT `ToastNotificationManager` via `CommunityToolkit.WinUI.Notifications` | Renders toast notifications; routes COM activation callbacks |

Diagram: `docs/diagrams/c2-containers.puml`

---

## Components (C4 L3) — BlinkNotifier.Core

`BlinkNotifier.Core` is the most behaviourally complex container; the others are thin (App =
shell + DI composition root; Settings = POCO + persistence).

| Component | Type | Purpose |
|---|---|---|
| `ReminderTimerService` | `IHostedService` | Schedules periodic ticks at the user-configured interval; gates on `ScheduleGuard`, `SnoozeStateMachine`, and `FullscreenState` before dispatching; resets timer after each notification event |
| `SnoozeStateMachine` | Singleton service | Thread-safe in-memory snooze state (`IsSnoozed`, `SnoozedUntil`); exposes `Snooze(duration)` and `Clear()`; snooze expiry detected by polling `IsSnoozed` in the timer loop |
| `ScheduleGuard` | Singleton service | Pure `ShouldFire(DateTimeOffset now, BlinkSettings s)` — checks current time against `ScheduleStartTime`/`ScheduleEndTime` and `ActiveDays`; no side effects |
| `FullscreenPoller` | `IHostedService` | 5-second poll loop; P/Invoke `GetForegroundWindow` → `GetWindowRect` → `MonitorFromWindow` → `GetMonitorInfo`; updates `FullscreenState`; publishes `FullscreenChanged` event on transition |
| `FullscreenState` | Singleton | In-memory `IsFullscreenActive` + `FullscreenEnteredAt`; written by `FullscreenPoller`, read by `ReminderTimerService` and `TrayIconViewModel` |
| `ToastDispatcher` | Service | Builds `ToastContent` via `ToastContentBuilder`; calls `ToastNotificationManagerCompat.CreateToastNotifier().Show()`; handles AUMID for both MSIX and portable (unpackaged) paths |
| `ToastActivationHandler` | Static callback | Registered once via `ToastNotificationManagerCompat.OnActivated`; parses `arguments` (`action=snooze;duration=5`, `action=dismiss`); routes to `SnoozeStateMachine.Snooze()` + `ResetTimer()` or `SnoozeStateMachine.Clear()` + `ResetTimer()` |
| `NativeMethods` | Static P/Invoke class | `GetForegroundWindow`, `GetWindowRect`, `MonitorFromWindow`, `GetMonitorInfo` from `user32.dll` |

Diagram: `docs/diagrams/c3-components-core.puml`

---

## Solution layout

```
src/
  BlinkNotifier.App/                  ← WPF project (.NET 10, OutputType=WinExe)
    App.xaml / App.xaml.cs           ← WPF Application; single-instance Mutex; builds Generic Host; owns IHost lifetime
    TrayIcon/
      TrayIconService.cs              ← wraps Hardcodet NotifyIcon; wires commands from ViewModel
      TrayIconViewModel.cs            ← Start/Stop/Settings/Exit; observes FullscreenState for PAUSED badge
    Settings/
      SettingsWindow.xaml             ← interval control, schedule toggles, startup toggle
      SettingsViewModel.cs            ← loads/saves via ISettingsStore; inline validation
    Wizard/
      FirstRunWizard.xaml             ← welcome + auto-launch confirmation; shown once on first run
    Startup/
      StartupRegistrar.cs             ← MSIX path: StartupTask API; portable path: HKCU Run key

  BlinkNotifier.Core/                 ← Class library (net10.0-windows10.0.17763.0)
    Timer/
      ReminderTimerService.cs         ← IHostedService; PeriodicTimer; full gate logic
      SnoozeStateMachine.cs           ← thread-safe; IsSnoozed / SnoozedUntil; Snooze() / Clear()
    Toast/
      ToastDispatcher.cs              ← ToastContentBuilder; ToastNotificationManagerCompat
      ToastActivationHandler.cs       ← static OnActivated; routes snooze / dismiss args
    Schedule/
      ScheduleGuard.cs                ← pure ShouldFire() function
    Fullscreen/
      FullscreenPoller.cs             ← IHostedService; 5-second PeriodicTimer
      FullscreenState.cs              ← IsFullscreenActive, FullscreenEnteredAt
      NativeMethods.cs                ← P/Invoke declarations
    CoreServiceExtensions.cs          ← IServiceCollection.AddBlinkCore()

  BlinkNotifier.Settings/             ← Class library (net10.0)
    BlinkSettings.cs                  ← POCO: all persisted fields + SchemaVersion
    ISettingsStore.cs                 ← Task<BlinkSettings> LoadAsync(); Task SaveAsync(BlinkSettings)
    JsonSettingsStore.cs              ← System.Text.Json; atomic write (temp + rename); migration guard
    BlinkSettingsValidator.cs         ← IValidateOptions<BlinkSettings>; range and business-rule checks
    SettingsServiceExtensions.cs      ← IServiceCollection.AddBlinkSettings()

  BlinkNotifier.Packaging/            ← Windows Application Packaging Project (.wapproj)
    Package.appxmanifest              ← MSIX identity, windows.startupTask extension, capabilities
    Images/                           ← Store icon PNGs (placeholder; replace with real artwork before Store submission)
      generate-icons.ps1              ← regenerates icon PNGs; run after updating source SVG

tests/
  BlinkNotifier.Core.Tests/           ← xUnit; ReminderTimerService, ScheduleGuard, SnoozeStateMachine, FullscreenState
  BlinkNotifier.Settings.Tests/       ← xUnit; JSON round-trip, validator, defaults
  BlinkNotifier.Integration.Tests/    ← xUnit; toast activation routing on Windows runner

docs/
  diagrams/
    c1-context.puml
    c2-containers.puml
    c3-components-core.puml
  privacy.html                        ← Store-required static privacy policy served via GitHub Pages

.github/
  workflows/
    ci.yml                            ← build + test + dotnet format check on every push/PR to main or feat/**
    release.yml                       ← publish portable EXE (.zip) + MSIX; gh release draft on v* tag push
    pages.yml                         ← deploy docs/ to GitHub Pages on push to main
```

**Dependency direction:** `BlinkNotifier.App` → `BlinkNotifier.Core` → `BlinkNotifier.Settings`.
No circular dependencies. `BlinkNotifier.Packaging` references `BlinkNotifier.App` as its entry-point project and does not add C# source.

**Epic-to-module traceability:**

| Epic (PLAN.md) | Primary module | Secondary module |
|---|---|---|
| 1 — Foundations | `BlinkNotifier.App` | `BlinkNotifier.Settings` |
| 2 — Core Reminder Loop | `BlinkNotifier.Core` (Timer, Toast) | `BlinkNotifier.App` (TrayIcon) |
| 3 — Settings & Schedule | `BlinkNotifier.Core` (Schedule) | `BlinkNotifier.App` (Settings), `BlinkNotifier.Settings` |
| 4 — Fullscreen Auto-Pause | `BlinkNotifier.Core` (Fullscreen) | `BlinkNotifier.App` (TrayIcon badge) |
| 5 — Distribution | `BlinkNotifier.Packaging` | `.github/workflows/ci.yml`, `docs/privacy.html` |

---

## Cross-cutting wiring

| Concern | Implementation | ADR |
|---|---|---|
| **AuthN / AuthZ** | None — single Windows OS user; all policies from PLAN granted unconditionally at runtime; no authorization checks in v1 | ADR-0009 |
| **Multi-tenancy** | None — single user, single device; no shared data model | ADR-0009 |
| **Observability** | `Serilog` → rolling JSON file sink (`%LOCALAPPDATA%\BlinkNotifier\logs\blink-.json`, 7-day retention); Windows Event Log sink for Error/Fatal entries (source: `Blink Notifier`); host-level `ILogger<T>` throughout | ADR-0010 |
| **Health checks** | In-process: `ReminderTimerService` logs a heartbeat at `Debug` level each tick; no HTTP health endpoint | ADR-0011 |
| **Resilience** | `PeriodicTimer` is intrinsically resilient to single-tick latency; toast dispatch wrapped in `try/catch` with error log; no Polly (no outbound network calls) | ADR-0011 |
| **Configuration** | Settings loaded at runtime via `ISettingsStore.LoadAsync()` (not `IOptions<T>`); validated before save by `SettingsViewModel.Validate()`; `BlinkSettingsValidator` (`IValidateOptions<BlinkSettings>`) is a formal test target; defaults come from `BlinkSettings` field initialisers and are written on first use | ADR-0013 |
| **Secrets** | None — the app contains no credentials, tokens, or keys | ADR-0012 |
| **Background jobs** | Two `IHostedService` registrations: `ReminderTimerService` (user-configured interval) and `FullscreenPoller` (5-second fixed interval); both hosted by .NET Generic Host | ADR-0007 |
| **Single-instance enforcement** | Global `Mutex` named `Global\\BlinkNotifier-SingleInstance` in `App.xaml.cs`; second instance calls `Application.Shutdown(0)` and exits silently (tray icon remains accessible) | ADR-0006 |
| **Outbox / idempotency** | Not required — all side effects are local and fire-and-forget; no external system whose delivery must be guaranteed | ADR-0016 |
| **GDPR / PII** | No personal data collected or transmitted; `[Pii]` attribute inapplicable; Store privacy policy at `docs/privacy.html` states "no data collected" | ADR-0009 |
| **Accessibility** | All WPF controls set `AutomationProperties.Name`; tray icon sets `ToolTipText`; toast action buttons inherit accessible names from button label text; labels verified by code review of XAML | SPEC regulatory constraint |

---

## Cloud topology

**Not applicable.** This app runs entirely on the user's Windows device. There is no cloud
deployment, no managed services, no container platform, no Terraform configuration, and no IaC.
See ADR-0012.

---

## Data model (concrete)

No relational database or vector store. All persistent state is a single JSON file.

**File location:** `%LOCALAPPDATA%\BlinkNotifier\settings.json`

**Persisted schema (`BlinkSettings.cs`):**

```csharp
public sealed class BlinkSettings
{
    public int SchemaVersion { get; init; } = 1;

    [Range(1, 60)]
    public int ReminderIntervalMinutes { get; init; } = 20;

    public bool ScheduleEnabled { get; init; } = false;
    public TimeSpan ScheduleStartTime { get; init; } = TimeSpan.FromHours(9);
    public TimeSpan ScheduleEndTime { get; init; } = TimeSpan.FromHours(18);
    public DayOfWeek[] ActiveDays { get; init; } =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
         DayOfWeek.Thursday, DayOfWeek.Friday];

    public bool AutoLaunchEnabled { get; init; } = true;
    public int[] SnoozeOptionsMinutes { get; init; } = [5, 15, 60];
}
```

**In-memory state (never persisted, reset on app restart):**

```csharp
// SnoozeStateMachine
bool IsSnoozed
DateTimeOffset SnoozedUntil

// FullscreenState
bool IsFullscreenActive
DateTimeOffset? FullscreenEnteredAt
```

**Write strategy:** `JsonSettingsStore.SaveAsync()` serialises to a temp file in the same
directory, then calls `File.Move(overwrite: true)` to swap it over the live file — prevents
corruption on crash mid-write and handles the first-save case where the destination doesn't yet exist.

**Migration guard:** `JsonSettingsStore.LoadAsync()` compares `SchemaVersion` against the
current constant; if older, a `Migrate(int from, BlinkSettings raw)` method transforms the raw
object before validation. No migration needed for v1 (initial schema); increment `SchemaVersion`
and add a migration branch for any future format change.

---

## API surface (concrete)

**Not applicable.** No HTTP endpoints, no REST API, no gRPC, no WebSocket. See ADR-0011.

The only external "API surface" is the Windows Toast COM activation entry point:

- **Inbound:** `ToastNotificationManagerCompat.OnActivated` static delegate receives
  `arguments` string (`action=snooze;duration=5`, `action=snooze;duration=15`,
  `action=snooze;duration=60`, `action=dismiss`) and `userInput` dictionary.
  Routes: `snooze` → `SnoozeStateMachine.Snooze(TimeSpan.FromMinutes(duration))` + `ResetTimer()`;
  `dismiss` → `SnoozeStateMachine.Clear()` + `ResetTimer()`.
  Toast expiry (no user interaction) is treated as `dismiss`.

---

## MAF agents

**Not applicable.** No AI features in v1. See ADR-0014.

---

## SPA architecture

**Not applicable.** No web frontend. The only UI is WPF: the settings window and first-run wizard.
See ADR-0015.

---

## Diagrams checked into the repo

- `docs/diagrams/c1-context.puml`
- `docs/diagrams/c2-containers.puml`
- `docs/diagrams/c3-components-core.puml`
