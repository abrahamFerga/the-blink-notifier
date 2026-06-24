# The Blink Notifier — Architecture Decision Records

ADRs are appended-only. Reversing a decision adds a new ADR that supersedes the old one (with a
back-reference); the old one is never deleted or renumbered.

---

## ADR-0001: WPF (.NET 9) over WinUI 3 as the UI framework

- **Status**: accepted
- **Date**: 2026-06-24
- **Deciders**: Abraham Fernandez

### Context

The app needs a system tray icon, a settings window, and a first-run wizard on Windows 10 and 11.
Two viable framework choices exist: WPF (.NET 9) and WinUI 3 (Windows App SDK 1.x). The tray
icon library and toast stack decisions (ADR-0002, ADR-0003) depend on this choice.

### Decision

We will use **WPF on .NET 9**. WPF supports Windows 10 1809+ (the minimum target), has the
mature `Hardcodet.NotifyIcon.Wpf` ecosystem for tray icons, requires no WinAppSdk bootstrapper,
and ships no additional redistributable beyond the .NET runtime already bundled in the
self-contained EXE.

### Consequences

- **Positive**: No WinAppSdk bootstrapper package or MSIX dependency for the tray icon; broader
  Windows 10 compat; `Hardcodet.NotifyIcon.Wpf` provides XAML context menu and data binding; WPF
  MVVM pattern is well understood; simpler CI pipeline (no WinAppSdk side-load step for tests).
- **Negative**: Windows 11 design language (Mica, rounded corners, WinUI controls) is not natively
  available; WinUI 3 would have provided `AppNotificationManager` directly.
- **Neutral**: Toast notification stack is still handled by `CommunityToolkit.WinUI.Notifications`
  which works with WPF (ADR-0003).

### Alternatives considered

- **WinUI 3 (Windows App SDK)** — native Windows 11 design language, `AppNotificationManager`
  built-in. Rejected because: requires WinAppSdk bootstrapper; `H.NotifyIcon` (the only WinUI 3
  tray lib) is community-maintained with lower ecosystem maturity; Windows 10 support is limited
  (WinAppSdk 1.x drops Win10 support in later releases); adds MSIX-only constraint for the tray
  shell, complicating the portable EXE path.
- **.NET MAUI** — cross-platform, WinUI backend on Windows. Rejected because: macOS/Linux are
  explicitly out of scope; MAUI's tray icon story on Windows is immature for v1.

---

## ADR-0002: Hardcodet.NotifyIcon.Wpf for the system tray icon

- **Status**: accepted
- **Date**: 2026-06-24
- **Deciders**: Abraham Fernandez

### Context

The app's only persistent UI surface is a system tray icon with a right-click context menu.
Given WPF (ADR-0001), a tray icon library must be chosen. Three options exist:
`Hardcodet.NotifyIcon.Wpf`, `H.NotifyIcon`, and raw `System.Windows.Forms.NotifyIcon` with a
WPF dispatcher bridge.

### Decision

We will use **Hardcodet.NotifyIcon.Wpf**. It provides a XAML-native `TaskbarIcon` control with
full WPF data binding, balloon/popup support, and a `ContextMenu` that accepts standard WPF
`MenuItem` elements — no dispatcher bridging required.

### Consequences

- **Positive**: XAML context menu with `ICommand` bindings; no `System.Windows.Forms` dependency;
  `ToolTipText` property directly exposes UIA accessible name; widely used (NuGet download count
  in millions).
- **Negative**: WPF-only (not portable to WinUI 3 if the framework choice changes).
- **Neutral**: Icon swapping (for PAUSED badge) done by replacing the `TaskbarIcon.Icon` property
  with a pre-rendered `.ico` asset at runtime — no native badge API available.

### Alternatives considered

- **H.NotifyIcon** — supports WPF, WinUI 3, and MAUI. Rejected because: community-maintained,
  smaller adoption base, and the multi-framework support is unnecessary for a WPF-only app.
- **System.Windows.Forms.NotifyIcon** — ships in-box. Rejected because: requires
  `ApplicationConfiguration.Initialize()` and STA thread coordination with WPF; no XAML context
  menu; `[Obsolete]` trajectory on .NET.

---

## ADR-0003: CommunityToolkit.WinUI.Notifications for toast dispatch

- **Status**: accepted
- **Date**: 2026-06-24
- **Deciders**: Abraham Fernandez

### Context

The app must fire Windows toast notifications with Snooze and Dismiss action buttons. Two
notification stacks exist: `Microsoft.Windows.AppNotifications` (WinAppSdk, requires MSIX package
identity) and `CommunityToolkit.WinUI.Notifications` (supports both packaged and unpackaged apps
via `ToastNotificationManagerCompat`). The app ships two artifacts: an MSIX package and a portable
self-contained EXE. The portable EXE has no MSIX identity, which affects which stack can fire
action-button toasts.

### Decision

We will use **CommunityToolkit.WinUI.Notifications** (`ToastContentBuilder` +
`ToastNotificationManagerCompat`). The `Compat` variant automatically falls back to AUMID-based
registration for unpackaged apps (ADR-0005), enabling full action-button toasts in both the MSIX
and portable EXE artifacts from the same code path.

### Consequences

- **Positive**: Single toast code path for both distribution artifacts; `ToastContentBuilder`
  fluent API reduces XML template boilerplate; `ToastNotificationManagerCompat.OnActivated`
  handles COM activation for both packaged and unpackaged apps.
- **Negative**: Extra NuGet dependency; action-button callbacks in the portable EXE require a COM
  AUMID registration step (ADR-0005) that the MSIX artifact does not need.
- **Neutral**: Toast history (`ToastNotificationManagerCompat.History`) works uniformly across
  both distribution paths.

### Alternatives considered

- **Microsoft.Windows.AppNotifications (WinAppSdk)** — native WinUI 3 toast API. Rejected
  because: requires MSIX package identity or a registered COM AUMID that must be present before
  `AppNotificationManager.Default.Register()` is called; does not simplify the unpackaged path
  compared to CommunityToolkit; would also require the WinAppSdk bootstrapper (see ADR-0001).
- **System.Windows.Forms.Application → BalloonTip** — legacy fallback for portable EXE.
  Rejected because: BalloonTip has no action buttons; the Snooze UI would degrade to a modal
  popup, breaking the one-click UX requirement in SPEC.

---

## ADR-0004: P/Invoke (GetForegroundWindow + GetWindowRect) for fullscreen detection

- **Status**: accepted
- **Date**: 2026-06-24
- **Deciders**: Abraham Fernandez

### Context

The app must detect when the foreground window occupies the full monitor bounds and suppress
notifications (Epic 4). Two detection strategies exist: a `user32.dll` P/Invoke chain
(`GetForegroundWindow` → `GetWindowRect` → `MonitorFromWindow` → `GetMonitorInfo`) and a DXGI
`IDXGISwapChain` exclusive-fullscreen query.

### Decision

We will use the **P/Invoke chain** (`user32.dll`). It catches the primary pain point for the
Desk Developer persona (browser videos and windowed-fullscreen media players) without requiring
COM interop with DXGI and without GPU-API access permissions.

### Consequences

- **Positive**: Simple static P/Invoke declarations; no COM interop; catches browser video
  fullscreen, media player fullscreen, and windowed-fullscreen applications; polling at 5-second
  intervals is negligible CPU cost.
- **Negative**: Does not detect `IDXGISwapChain` exclusive-fullscreen for DX11/DX12 games when
  the game window rect does not match monitor bounds (borderless-window games are covered; true
  exclusive-fullscreen with a custom resolution may return an undersized `GetWindowRect`). This
  is an acceptable miss for v1 — the primary complaint in competitive app reviews is browser
  video, not gaming.
- **Neutral**: A DXGI-based upgrade path exists for v2 without changing the service interface —
  `FullscreenPoller` is the only consumer of `NativeMethods`.

### Alternatives considered

- **DXGI IDXGISwapChain exclusive-fullscreen query** — precise for DX games with exclusive
  fullscreen. Rejected because: requires COM interop with DXGI; GPU API access may be blocked
  by driver security policies; overkill for v1's target persona (browser video, not AAA gaming).
- **Windows Shell notification hooks (WinEvent / SetWinEventHook)** — event-driven, lower
  latency. Rejected because: requires an unmanaged native DLL or complex WinProc marshalling;
  event-driven shell hooks are complex to implement reliably in a managed WPF process.

---

## ADR-0005: Portable EXE self-registers a COM AUMID on first launch

- **Status**: accepted
- **Date**: 2026-06-24
- **Deciders**: Abraham Fernandez

### Context

`ToastNotificationManagerCompat` (ADR-0003) requires an Application User Model ID (AUMID) for
unpackaged apps to fire toasts with Snooze/Dismiss action buttons. The MSIX artifact acquires its
AUMID from `AppxManifest` automatically. The portable self-contained EXE has no MSIX identity.

### Decision

The portable EXE will **self-register its AUMID on first launch** by writing to
`HKCU\Software\Classes\AppUserModelId\BlinkNotifier.App` (the key `CommunityToolkit.WinUI.Notifications`
reads). `StartupRegistrar.RegisterAumidAsync()` is called at app startup before
`ToastNotificationManagerCompat.OnActivated` is wired. No installer is required; no elevated
permissions are needed (HKCU is per-user).

### Consequences

- **Positive**: Full action-button toasts in the portable EXE with no separate installer; HKCU
  write requires no elevation; `ToastNotificationManagerCompat` handles the rest.
- **Negative**: The AUMID entry persists in the registry after uninstall of the portable EXE
  (no uninstaller exists); this is a benign orphan key in HKCU.
- **Neutral**: The MSIX artifact does not call `RegisterAumidAsync()` — the code path is guarded
  by `DesktopBridge.IsRunningAsPackaged()`.

### Alternatives considered

- **BalloonTip fallback for portable EXE** — degrade to `System.Windows.Forms.Application` balloon
  with no action buttons. Rejected because: Snooze action buttons are a hard requirement in SPEC
  (PLAN.md Q3 resolution); UX parity between MSIX and portable is the goal.
- **Require MSIX for all users (single artifact)** — eliminates the portable EXE path. Rejected
  because: SPEC Q4 resolution explicitly chose both artifacts; some users avoid the Store and
  prefer a direct download.

---

## ADR-0006: Global named Mutex for single-instance enforcement

- **Status**: accepted
- **Date**: 2026-06-24
- **Deciders**: Abraham Fernandez

### Context

The app must run as a single instance — a second launch (e.g. from startup) must not create a
second tray icon. Single-instance enforcement in WPF can be implemented via a global `Mutex`, a
named pipe, or the `SingleInstance` pattern from `Microsoft.Shell`.

### Decision

We will use a **global named `Mutex`** (`Global\BlinkNotifier-SingleInstance`) created in
`Program.cs` before the Generic Host is built. A second launch detects the mutex is owned,
sends a `WM_USER` window message to bring the first instance's settings window to the foreground,
and exits with code 0.

### Consequences

- **Positive**: Zero extra dependencies; robust across session boundaries (the `Global\` prefix
  makes the mutex machine-wide); straightforward lifetime tied to the process.
- **Negative**: `WM_USER` message dispatch requires a hidden message window (`HwndSource`) in the
  first instance to receive the activation signal; adds a small amount of Win32 wiring.
- **Neutral**: The mutex name is a constant; if the MSIX and portable EXE are both installed, they
  share the mutex name and correctly prevent two coexisting instances.

### Alternatives considered

- **Named pipe** — richer IPC for future "bring to foreground + pass args". Rejected because:
  overkill for v1's single need (prevent duplicate instances); adds async pipe server setup.
- **Microsoft.Shell SingleInstance / WindowsFormsApplicationBase** — library-level solution.
  Rejected because: `WindowsFormsApplicationBase` is in the WinForms compatibility layer; adds a
  WinForms dependency to a WPF app.

---

## ADR-0007: .NET 9 Generic Host (IHostBuilder) as the application backbone

- **Status**: accepted
- **Date**: 2026-06-24
- **Deciders**: Abraham Fernandez

### Context

The app has two long-running background services (`ReminderTimerService`,
`FullscreenPoller`), structured logging, and `IOptions<T>` configuration — all of which the .NET
Generic Host (`Microsoft.Extensions.Hosting`) provides out of the box. The alternative is a
bare WPF `App.xaml.cs` with manual lifecycle management.

### Decision

We will use the **.NET 9 Generic Host** (`Host.CreateApplicationBuilder()`) as the DI container,
configuration system, logging infrastructure, and `IHostedService` runner. The WPF pump is
started inside `IHostedService.StartAsync()` on the STA thread; `IHostApplicationLifetime` drives
graceful shutdown.

### Consequences

- **Positive**: `IHostedService` for both background workers; `IOptions<T>` with startup
  validation; `ILogger<T>` unified with Serilog sink; clean `StopAsync()` cancellation via
  `CancellationToken`; no manual thread lifecycle code.
- **Negative**: The WPF STA thread and the Generic Host's thread-pool async model require careful
  threading — the WPF dispatcher must not be blocked from a `HostedService`; all UI interactions
  route through `Dispatcher.InvokeAsync()`.
- **Neutral**: The Generic Host's default `appsettings.json` configuration provider is disabled;
  configuration comes exclusively from `JsonSettingsStore` via a custom `IConfigurationSource`.

### Alternatives considered

- **Bare WPF App.xaml.cs with manual threading** — no dependency on `Microsoft.Extensions.Hosting`.
  Rejected because: manual `ILogger`, `IOptions<T>`, and `CancellationToken` plumbing duplicates
  what the host provides; background service lifetime management would be bespoke.
- **Aspire AppHost** — Aspire resource orchestration. Rejected because: Aspire is designed for
  distributed cloud apps with multiple processes; a single-process Windows desktop app has no use
  for it. Inapplicability recorded per PLAN.md enterprise guardrail table.

---

## ADR-0008: %LOCALAPPDATA% over %APPDATA%\Roaming for the settings file

- **Status**: accepted
- **Date**: 2026-06-24
- **Deciders**: Abraham Fernandez

### Context

Windows offers two per-user app-data paths: `%APPDATA%\Roaming` (synced across domain-joined
machines via Folder Redirection) and `%LOCALAPPDATA%` (device-local). The settings path must be
chosen before `JsonSettingsStore` is implemented.

### Decision

We will store `BlinkSettings.json` at **`%LOCALAPPDATA%\BlinkNotifier\settings.json`**. The
Blink Notifier is a personal wellness tool; silently syncing reminder intervals and schedule
windows across domain-joined machines is undesirable behaviour.

### Consequences

- **Positive**: Settings stay on the device they were configured on; no unintended sync side
  effects in enterprise environments.
- **Negative**: Settings are not automatically available on a second Windows device; the user
  must configure each machine independently.
- **Neutral**: The Chronic Dry Eye Sufferer persona (who has a prescribed interval) typically
  uses one primary work machine; no sync need has been expressed.

### Alternatives considered

- **%APPDATA%\Roaming** — settings sync across domain-joined machines. Rejected because: wellness
  tool settings are personal and device-contextual; silent sync could produce unexpected
  notification behaviour on machines the user didn't configure.
- **Registry (HKCU)** — no file dependency. Rejected because: JSON is human-readable, easily
  backed up, and straightforward to migrate; registry access requires different tooling for
  backup/restore and is harder to inspect during debugging.

---

## ADR-0009: No OIDC, no multi-tenancy, no GDPR data-export flow

- **Status**: accepted
- **Date**: 2026-06-24
- **Deciders**: Abraham Fernandez

### Context

Enterprise guardrails require OIDC authentication (Entra ID / Cognito), EF Core multi-tenancy
query filters, and a GDPR data-export endpoint. The Blink Notifier is a single-user local desktop
app with no server, no database, and no personal data.

### Decision

**All identity, multi-tenancy, and GDPR-flow guardrails are inapplicable to this system.**
The Windows OS user account provides the identity boundary. No auth checks are performed at
runtime in v1 (all PLAN.md policy names are reserved for a hypothetical v2 enterprise tier).
No personal data is collected; the Store privacy policy at `docs/privacy.html` explicitly states
"no data collected or transmitted."

### Consequences

- **Positive**: No login screen, no account creation, no session management code to maintain.
- **Negative**: No v1 path to enforce administrator-pushed interval policies (reserved for v2).
- **Neutral**: SPEC's FDA SaMD wellness-framing constraint still applies; it is enforced via
  copy review, not code.

### Alternatives considered

- **Add optional Microsoft Account sign-in for future sync** — enables cross-device settings
  sync. Rejected because: SPEC explicitly mandates "fully offline, no account"; adding optional
  auth would require shipping MSAL, a privacy policy update, and a sign-in UI — all out of scope.
- **Local machine-account RBAC checks** — check Windows user SID on sensitive operations.
  Rejected because: the app runs as the logged-in user with no elevated operations; no other user
  account can interact with the tray icon; OS process isolation is sufficient.

---

## ADR-0010: Serilog local logging over OpenTelemetry distributed stack

- **Status**: accepted
- **Date**: 2026-06-24
- **Deciders**: Abraham Fernandez

### Context

Enterprise guardrails require OpenTelemetry via Aspire `ServiceDefaults` and distributed traces
exported to a remote sink. The Blink Notifier is a single-process desktop app with no network
connectivity and no remote telemetry endpoint.

### Decision

We will use **Serilog** with two sinks: (1) a rolling JSON file at
`%LOCALAPPDATA%\BlinkNotifier\logs\blink-.json` (daily rotation, 7-file retention) for structured
debug logs; (2) `Serilog.Sinks.EventLog` for `Error`+`Fatal` events so Partner Center crash
analytics can correlate Windows Event Log entries. No OTel exporter, no remote sink, no
distributed trace context.

### Consequences

- **Positive**: Local logs are accessible without a telemetry backend; Windows Event Log
  integration provides crash visibility via Partner Center for MSIX-packaged installs.
- **Negative**: No distributed traces, no metrics dashboard, no alerting. Acceptable for a
  single-process desktop app with no SLA.
- **Neutral**: If a v2 enterprise tier adds a management server, OTel can be added to the host
  at that point without changing the `ILogger<T>` call sites.

### Alternatives considered

- **OpenTelemetry with file exporter** — OTel SDK writing OTLP to a local file. Rejected because:
  adds OTel SDK dependency with no corresponding benefit (no collector, no backend, no query tool
  for the end user); Serilog JSON files are simpler for local diagnostics.
- **No logging** — keep the binary small. Rejected because: structured crash logs are the
  primary diagnostic tool for Store reviews and GitHub issue reports; Partner Center analytics
  require Event Log entries.

---

## ADR-0011: No HTTP API, no Problem Details, no Polly, no rate limiting

- **Status**: accepted
- **Date**: 2026-06-24
- **Deciders**: Abraham Fernandez

### Context

Enterprise guardrails require a URL-versioned HTTP API with Problem Details (RFC 7807),
idempotency keys, Polly resilience handlers, per-tenant rate limiting, and explicit CORS.
The Blink Notifier has no HTTP endpoints and makes no outbound HTTP calls.

### Decision

**All HTTP API and network resilience guardrails are inapplicable.** No ASP.NET Core, no Polly
NuGet package, and no `HttpClient` are added to any project. The only "external" surface is the
Windows Toast COM activation callback (see ARCH.md API surface section).

### Consequences

- **Positive**: No HTTP stack in the binary; smaller install size; no CORS, rate-limiting, or
  middleware configuration to maintain.
- **Negative**: None — these guardrails exist for server-side APIs; a desktop app with no server
  has no use for them.
- **Neutral**: Toast COM activation error handling uses `try/catch` + Serilog log rather than
  Polly circuit breakers — appropriate for a local, non-network side effect.

### Alternatives considered

- **Named-pipe local API** — expose a local IPC channel so future tools (CLI, widget) can control
  the timer. Rejected because: no such client exists in v1; premature abstraction; the Generic
  Host already provides a sufficient `IHostedService` control surface internally.
- **gRPC over named pipe** — typed IPC. Rejected for the same reason as the above.

---

## ADR-0012: No cloud topology, no Terraform, no IaC

- **Status**: accepted
- **Date**: 2026-06-24
- **Deciders**: Abraham Fernandez

### Context

Enterprise guardrails require a cloud provider (Azure or AWS), Terraform IaC, managed identity,
a secret store (Key Vault or Secrets Manager), and a container or serverless compute target.
`workflow.json` declares `"cloud": "none"`.

### Decision

**Cloud topology and IaC are inapplicable.** The app is distributed as a Windows binary; no
server-side infrastructure exists. GitHub Actions CI/CD publishes artifacts to GitHub Releases
and the Microsoft Store — no cloud compute, no managed database, no secret store.

### Consequences

- **Positive**: Zero infrastructure cost; zero cloud vendor dependency; zero operational burden
  on the developer beyond GitHub and Partner Center accounts.
- **Negative**: No server-side feature flags, no remote configuration push, no usage telemetry
  beyond what Partner Center provides for MSIX apps.
- **Neutral**: A future v2 enterprise tier (administrator-pushed interval policies) would require
  revisiting this decision and introducing a management service — that is a new system, not an
  extension of the v1 architecture.

### Alternatives considered

- **Azure Static Web App for settings sync** — store settings in the cloud for cross-device sync.
  Rejected because: SPEC mandates fully offline with no account; any cloud backend requires auth.
- **Azure App Configuration** — remote feature flags. Rejected because: the app makes zero
  network calls at runtime; remote config would violate the offline constraint.

---

## ADR-0013: System.Text.Json + JSON file over Postgres and EF Core

- **Status**: accepted
- **Date**: 2026-06-24
- **Deciders**: Abraham Fernandez

### Context

Enterprise guardrails require Postgres as the default relational store with EF Core multi-tenancy
filters. The Blink Notifier has a single settings object with eight scalar fields and no
relational data.

### Decision

We will use **`System.Text.Json` serialisation to a local JSON file** (`BlinkSettings.json`).
No database engine, no ORM, no migrations. `System.Text.Json` ships in the .NET 9 runtime; no
additional dependency is required.

### Consequences

- **Positive**: Zero database setup; zero migration tooling; settings file is human-readable and
  editable in a text editor; no Postgres service dependency.
- **Negative**: No query language; no atomic multi-record writes (mitigated by the atomic
  temp-file-rename write strategy in `JsonSettingsStore`); schema migrations are hand-coded.
- **Neutral**: The `SchemaVersion` field in `BlinkSettings` provides a forward-compatible
  migration guard.

### Alternatives considered

- **SQLite via EF Core** — embedded relational store, no server required. Rejected because: the
  data model is a single settings object; SQLite + EF Core is significant complexity for eight
  scalar fields; JSON is more appropriate for a settings file than a relational table.
- **Windows Registry (HKCU)** — no file dependency. Rejected because: registry storage is opaque,
  not human-readable, harder to inspect during development, and lacks the `SchemaVersion`
  migration pattern; JSON is the established convention for app settings files.

---

## ADR-0014: No MAF agents in v1

- **Status**: accepted
- **Date**: 2026-06-24
- **Deciders**: Abraham Fernandez

### Context

Enterprise guardrails require AI agentic features built on the Microsoft Agent Framework (MAF).
The Blink Notifier has no AI features, no chatbot, and no LLM integration in v1.

### Decision

**MAF agents are inapplicable to v1.** No `Microsoft.Agent.Framework` packages are added to any
project.

### Consequences

- **Positive**: No LLM API key, no model costs, no latency from inference calls.
- **Negative**: None — no AI feature is specified in SPEC for v1.
- **Neutral**: A hypothetical v2 "smart scheduling" feature (adjust interval based on usage
  patterns) could integrate MAF without changing the existing architecture — `ReminderTimerService`
  already accepts `BlinkSettings` via `IOptions<T>`.

### Alternatives considered

- **Local LLM for adaptive scheduling** — run a small local model to adjust the interval. Rejected
  because: out of scope for v1; adds significant binary size and latency risk.
- **Cloud LLM API for usage advice** — suggest optimal interval based on session length. Rejected
  because: SPEC mandates fully offline with no network calls.

---

## ADR-0015: No React SPA — WPF settings window only

- **Status**: accepted
- **Date**: 2026-06-24
- **Deciders**: Abraham Fernandez

### Context

Enterprise guardrails require a Vite + React + TypeScript + shadcn/ui frontend. The Blink
Notifier's only UI surface is a system tray icon, a settings window, and a first-run wizard —
all of which are native desktop UI.

### Decision

**No web frontend.** The settings window and first-run wizard are implemented as WPF `Window`
classes using MVVM. No Vite, no React, no shadcn/ui, no Tailwind CSS.

### Consequences

- **Positive**: No Node.js toolchain dependency; no bundler; settings window renders as a native
  Windows dialog (title bar, window chrome, focus model consistent with other Windows apps).
- **Negative**: Settings UI is not web-accessible; a future browser-extension or web companion
  is not possible without a separate frontend project.
- **Neutral**: WPF MVVM (`INotifyPropertyChanged` + `ICommand`) is the established pattern for
  the settings ViewModel; no third-party MVVM framework is required for v1's simple two-window UI.

### Alternatives considered

- **WebView2 embedded in WPF** — host a web UI in a `WebView2` control for a modern look.
  Rejected because: adds WebView2 runtime dependency; introduces a JS bundle build step; the
  settings UI is simple enough that native WPF controls are cleaner.
- **WinUI 3 XAML islands in WPF** — modern Windows 11 controls in a WPF window. Rejected because:
  XAML islands require WinAppSdk bootstrapper (ADR-0001); overkill for a settings window.

---

## ADR-0016: No outbox pattern — all side effects are local and fire-and-forget

- **Status**: accepted
- **Date**: 2026-06-24
- **Deciders**: Abraham Fernandez

### Context

Enterprise guardrails require the outbox pattern for background jobs with external side effects
to guarantee at-least-once delivery. The Blink Notifier's background jobs produce three side
effects: firing a toast notification, updating in-memory state, and writing `BlinkSettings.json`.

### Decision

**No outbox pattern is required.** All side effects are local: (1) toast dispatch is local
fire-and-forget via WinRT — a missed notification has no durable external consequence; (2) snooze
and fullscreen states are in-memory and intentionally reset on restart; (3) `BlinkSettings.json`
writes use an atomic temp-rename strategy that prevents corruption but do not need at-least-once
delivery semantics.

### Consequences

- **Positive**: No transactional message store; no outbox table; no polling consumer; simpler
  `ReminderTimerService` implementation.
- **Negative**: A missed toast notification (e.g. due to a crash mid-dispatch) is not retried.
  This is acceptable — the next timer tick will fire a new notification.
- **Neutral**: The `SchemaVersion` field in `BlinkSettings` and the atomic write strategy in
  `JsonSettingsStore` together provide the only durability guarantee needed for this system.

### Alternatives considered

- **SQLite outbox for toast dispatch** — persist intent to fire before firing; retry on restart.
  Rejected because: a missed toast is not a business-critical event; adding SQLite for at-most-once
  retry is disproportionate complexity for a wellness notification.
- **Windows Task Scheduler** — register a scheduled task to fire the toast. Rejected because:
  Task Scheduler requires elevated setup; the Generic Host `IHostedService` timer is simpler and
  sufficient.
