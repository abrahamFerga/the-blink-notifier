# The Blink Notifier — Plan

## Epics (in build order)

1. **Foundations** — App host (Generic Host / `IHostBuilder`), single-instance enforcement, system
   tray shell (tray icon + right-click context menu: Start / Stop / Settings / Exit), first-run
   wizard (settings window skeleton + auto-launch confirmation prompt), Windows startup registration
   (MSIX startup task + portable registry fallback), settings persistence skeleton
   (`BlinkSettings.json` schema, default values, `IOptions<T>` validation), local structured error
   logging, UI Automation accessibility labels on all controls.
   Capabilities (from SPEC): System tray presence, Windows startup auto-launch, Fully offline / no
   account (foundational constraints — no auth, no network, no account).
   _Note: enterprise Foundations components that don't apply to a local single-user desktop app
   (OIDC, multi-tenancy, OpenTelemetry distributed traces, connector registry) are formally
   inapplicable here — each deviation is recorded as an ADR in `DECISIONS.md` during
   `/architecture:design-architecture`._

2. **Core Reminder Loop** — Interval timer engine (background hosted service), Windows toast
   notification dispatch with 20-20-20 instruction text, action buttons (Snooze 5 / 15 / 60 min,
   Dismiss — fixed durations per SPEC Q3 resolution), snooze state machine, toast activation COM
   callback handler, dismiss handling, graceful timer restart after snooze expires.
   Capabilities (from SPEC): Timer-based break reminders, Dismissible toast with snooze.
   Depends on: Foundations (app host, settings model, tray icon state).

3. **Settings & Schedule** — Configurable interval control (1–60 min, default 20 min, persisted),
   schedule model (single daily time window: start time + end time, applied to all selected days of
   week — per SPEC Q2 resolution), day-of-week checkboxes (Mon–Fri default), schedule guard
   (inline check before each timer fire), settings screen in the first-run wizard and via tray
   Settings action, persistence to `BlinkSettings.json`.
   Capabilities (from SPEC): Configurable reminder interval, Schedule-based activation.
   Depends on: Foundations (settings persistence skeleton), Core Reminder Loop (timer integration).

4. **Fullscreen Auto-Pause** — Windows foreground-window fullscreen detection via P/Invoke
   (`GetForegroundWindow` + `GetWindowRect` vs. `GetMonitorInfo` bounds), 5-second background
   polling loop, timer suppression on fullscreen enter, auto-resume on fullscreen exit, tray icon
   overlay badge ("PAUSED") while suppressed.
   Capabilities (from SPEC): Fullscreen auto-pause (differentiator).
   Depends on: Core Reminder Loop (timer engine — suppress/resume hooks).

5. **Distribution** — MSIX packaging project (AppxManifest with startup task extension, package
   identity, app capabilities), portable self-contained EXE artifact (single-file publish, no MSIX
   required — per SPEC Q4 resolution), GitHub Actions CI workflow (build → test → sign MSIX →
   upload GitHub Release assets), GitHub Pages privacy policy at `docs/privacy.html` (per SPEC Q5
   resolution), Microsoft Store submission checklist and age-rating declaration.
   Capabilities (from SPEC): GitHub Releases + Microsoft Store distribution (from user answers).
   Depends on: all previous epics with passing tests.

---

## Module list

| Module (.NET project name) | Bounded context | Capabilities served | Skills used to build it |
|---|---|---|---|
| `BlinkNotifier.App` | Shell | System tray presence, Windows startup auto-launch, first-run wizard | build-system |
| `BlinkNotifier.Core` | Reminder + Scheduling + Presence detection | Timer-based break reminders, Dismissible toast with snooze, Schedule-based activation, Fullscreen auto-pause | build-system |
| `BlinkNotifier.Settings` | Configuration | Configurable reminder interval, Fully offline / no account (settings model + persistence) | build-system |
| `BlinkNotifier.Packaging` | Distribution | GitHub Releases + Microsoft Store distribution | build-system |

Dependency direction: `BlinkNotifier.App` → `BlinkNotifier.Core` → `BlinkNotifier.Settings`.
`BlinkNotifier.Packaging` references `BlinkNotifier.App` as its entry point project.

---

## Data model sketch

No relational database. All persistent state is a single local JSON file.

- **BlinkSettings** (persisted at `%LOCALAPPDATA%\BlinkNotifier\settings.json`) — `ReminderIntervalMinutes` (int, 1–60), `ScheduleEnabled` (bool), `ScheduleStartTime` (TimeSpan), `ScheduleEndTime` (TimeSpan), `ActiveDays` (DaysOfWeek flags enum, default Mon–Fri), `AutoLaunchEnabled` (bool), `SnoozeOptions` (int[], minutes — default [5, 15, 60]), `SchemaVersion` (int, migration guard for future format changes). No PII fields; file is not encrypted (contains no credentials or health data).

- **SnoozeState** (in-memory only, not persisted) — `IsSnoozed` (bool), `SnoozedUntil` (DateTimeOffset). Reset on app restart; cleared when timer fires after snooze expiry or user dismisses next toast.

- **FullscreenState** (in-memory only, not persisted) — `IsFullscreenActive` (bool), `FullscreenEnteredAt` (DateTimeOffset?). Driven by the 5-second poll loop in Epic 4.

No multi-tenancy boundary, no audit trail, no PII tagging required (no data of any kind leaves the device).

> **`%LOCALAPPDATA%` vs `%APPDATA%\Roaming`:** Device-local path chosen (SPEC Q5 resolution). Roaming would sync settings across domain-joined machines silently — undesirable for a personal wellness tool. Record as ADR.

---

## RBAC model (refined)

Single-user local desktop app. No server-side authorization engine. Policy names are defined now
for forward-compatibility with a future enterprise tier (v2: administrator-pushed interval policies
to enrolled Windows machines).

| Role | Policies | Notes |
|---|---|---|
| User (local Windows account) | `Reminders.Start`, `Reminders.Stop`, `Reminders.Snooze`, `Reminders.Dismiss`, `Settings.Read`, `Settings.Write`, `Shell.ManageStartup`, `Shell.Quit` | All policies granted unconditionally to the running user. No authorization checks at runtime in v1 — single actor by OS definition. Policy names reserved for v2 enforcement layer. |

---

## Integration surface

No external connectors declared in `workflow.json` (`connectors: []`, `cloud: none`). All
integrations are with local Windows platform APIs.

| Integration | Direction | Purpose | API / Route | Per-session config |
|---|---|---|---|---|
| Windows Toast Notification Manager | outbound (local) | Fire 20-20-20 reminder toasts with Snooze / Dismiss action buttons | WinRT `ToastNotificationManager` / WinAppSdk `AppNotificationManager` | Requires AUMID (MSIX identity or registered COM server for portable EXE) |
| Windows Toast COM Activation | inbound (local) | Receive Snooze / Dismiss action callbacks | `INotificationActivationCallback` COM activation | Activated by `explorer.exe`; app must handle re-entry if launched by COM |
| MSIX Startup Task | outbound (registration, one-time) | Register app to launch with Windows | `AppxManifest` `windows.startupTask` extension | Portable fallback: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` |
| Windows Foreground Window API | inbound read (local) | Poll for fullscreen window state every 5 s | `user32.dll` P/Invoke: `GetForegroundWindow`, `GetWindowRect`, `GetMonitorInfo` | No config; monitor layout resolved at poll time |
| GitHub Actions / gh CLI | outbound (CI only, build-time) | Publish MSIX + portable EXE as GitHub Release assets | `gh release create` | Build workflow only; not a runtime integration |
| GitHub Pages | outbound (static, one-time setup) | Serve `docs/privacy.html` as Store-required privacy policy URL | `gh-pages` branch | Manual setup; not a runtime call |

---

## Background work

| Job | Trigger | Cadence | Outbox required? |
|---|---|---|---|
| ReminderTimer | Scheduled | User-configured interval (1–60 min, default 20 min); paused during snooze or fullscreen | No — toast dispatch is local fire-and-forget; a missed fire has no durable external side effect |
| ScheduleFilter | Reactive (inline, before each timer fire) | Evaluated on every timer tick | No — read-only comparison of current time vs BlinkSettings; no side effect |
| FullscreenPoller | Scheduled | Every 5 seconds | No — read-only P/Invoke; updates in-memory FullscreenState only; no external system involved |
| ToastActivationHandler | Reactive (COM activation by explorer.exe) | On user action (Snooze or Dismiss) | No — updates in-memory SnoozeState and resets/defers the timer; no durable write to an external system |

No outbox pattern required in v1: every "side effect" is local (fire a toast, update in-memory state,
write `BlinkSettings.json`). There are no network calls whose at-least-once delivery must be guaranteed.

---

## Enterprise guardrail adaptations

The Blink Notifier is a local single-user desktop app with no server, no database, no HTTP API, and
no network calls. The following standard guardrail components are formally inapplicable. Each must be
recorded as an ADR in `DECISIONS.md` during `/architecture:design-architecture` so the deviation is
documented rather than silently dropped.

| Guardrail | Status | Adaptation for this system |
|---|---|---|
| Identity & access (OIDC, Entra ID / Cognito, multi-tenancy) | N/A | Single Windows OS user; no login; OS provides identity boundary |
| Multi-tenancy (tenant ID on domain tables, EF query filters) | N/A | Single user, single machine; no shared data model |
| OpenTelemetry / Aspire `ServiceDefaults` (distributed traces, metrics) | Adapted | Local structured logging to a rolling file + Windows Event Log for crashes; no distributed telemetry stack |
| API surface (URL versioning, Problem Details, idempotency keys, CORS, rate limiting) | N/A | No HTTP endpoints |
| Resilience (Polly, Redis distributed cache) | N/A | No outbound network calls; in-process state only |
| Cloud secret store | N/A | No credentials; settings contain no secrets |
| GDPR data-export endpoint + PII `[Pii]` attribute | N/A | No personal data collected or stored; Store privacy policy explicitly states "no data collected" |

---

## Open questions for design-architecture

1. **UI framework (ADR-001)**: WinUI 3 (WinAppSdk 1.x) vs WPF (.NET 9). WinUI 3 provides
   `AppNotificationManager` (native WinAppSdk toast API) and Windows 11 design language; WPF has
   the mature `NotifyIcon` ecosystem (`Hardcodet.NotifyIcon.Wpf`), broader Win10 compat, and no
   extra WinAppSdk bootstrapper requirement. Choose one and record rationale — this choice gates
   the tray icon library and toast stack ADRs below.

2. **Tray icon library (ADR-002)**: `Hardcodet.NotifyIcon.Wpf` (WPF-only, widely used) vs
   `H.NotifyIcon` (WPF + WinUI3 + MAUI, community-maintained) vs raw `System.Windows.Forms.NotifyIcon`
   with a WPF dispatcher bridge. Affects install size and COM threading model.

3. **Toast notification stack (ADR-003)**: `Microsoft.Windows.AppNotifications` (WinAppSdk,
   requires MSIX package identity or a registered COM AUMID for unpackaged apps) vs
   `CommunityToolkit.WinUI.Notifications` (supports both packaged and unpackaged — relevant for the
   portable EXE artifact). The choice determines whether the portable self-contained EXE can fire
   toast notifications without MSIX, and whether Snooze/Dismiss action buttons survive the COM
   activation lifecycle on Windows 10.

4. **Fullscreen detection fidelity (ADR-004)**: `GetForegroundWindow` + `GetWindowRect`-vs-monitor
   bounds (simple P/Invoke; catches browser video and most exclusive-fullscreen games) vs DXGI
   `IDXGISwapChain` exclusive-fullscreen state (precise for DX games, complex COM interop).
   Recommend P/Invoke path for v1 (Desk Developer's pain is primarily browser video, not AAA games);
   confirm during architecture.

5. **Portable EXE toast identity**: The portable `.zip` artifact has no MSIX identity, so
   `AppNotificationManager` requires a registered COM AUMID via the registry. Determine whether
   the portable installer script handles this registration, or whether the portable EXE degrades to
   `System.Windows.Forms.Application.SetCompatibleTextRenderingDefault` + `BalloonTip`
   (legacy, no action buttons). This is a UX parity question between the two distribution artifacts.
