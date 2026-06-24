# The Blink Notifier — Product specification

## In one sentence

A zero-friction Windows system-tray app that fires a gentle, dismissible 20-20-20 toast notification
on a configurable timer, so screen-bound knowledge workers never forget to blink without disrupting
their flow.

---

## Primary jobs to be done

- When I'm deep in a coding or writing session, I want to be reminded to blink and look away every 20 minutes, so that I avoid dry eyes and headaches without having to think about it.
- When I finish work for the day, I want reminders to stop automatically, so that evening and weekend notifications don't annoy me.
- When a reminder fires at a bad moment (meeting, video, focus sprint), I want to snooze it in one click, so that I stay in control without disabling the whole app.
- When I restart my computer, I want the reminder to be running without any manual action, so that I never have a screen session without eye protection.

---

## Target personas

- **Desk Developer** — spends 8+ hours/day coding on Windows; wants reminders that are invisible
  until they fire and take one click to handle without breaking focus. Top 3 tasks:
  1. Launch Windows and find the app already running in the system tray with no action needed.
  2. Snooze a reminder with one click when deep in a build or Zoom call.
  3. Pause the timer completely when stepping away from the desk for an extended break.

- **Office Professional** — works 9–5 on a Windows PC; has no tolerance for reminders on evenings
  or weekends and wants set-and-forget scheduling. Top 3 tasks:
  1. Configure a work-hours window (e.g., 8 AM–6 PM Mon–Fri) once and never touch it again.
  2. Dismiss a notification with one click when already taking a break.
  3. Install and configure the app in under 2 minutes via Microsoft Store.

- **Chronic Dry Eye Sufferer** — has a clinical recommendation to blink more often; uses this as
  a compliance tool and cares that it works reliably and privately. Top 3 tasks:
  1. Set the interval to the frequency their optometrist prescribed (e.g., every 10 minutes).
  2. Trust that notifications fire reliably even during video playback and full-screen apps.
  3. Verify that no personal data leaves the device (offline-only app).

---

## Capabilities

### Must have (v1)

| Capability | One-line description | Personas |
|---|---|---|
| Timer-based break reminders | Fires a Windows toast notification at the user's configured interval; the core product loop. | All three |
| Dismissible toast with snooze | Windows toast shows 20-20-20 instruction text; action buttons for Dismiss and Snooze (5 / 15 / 60 min options). | Desk Developer, Office Professional |
| Configurable reminder interval | User sets interval from 1–60 minutes; default 20 min; persists across app restarts. | Office Professional, Chronic Dry Eye Sufferer |
| System tray presence | Single tray icon is the only persistent UI; right-click shows Start / Stop / Settings; no taskbar window during normal operation. | Desk Developer |
| Schedule-based activation | Suppresses notifications outside a configured daily time window and day-of-week selection; default is always-on. | Office Professional, Desk Developer |
| Windows startup auto-launch | App registers as a Windows startup entry on install; confirmed in first-run wizard; toggle in Settings. | All three |
| Fully offline, no account | All state stored in local config files; zero network calls; no sign-up, no telemetry. | Chronic Dry Eye Sufferer |

### Differentiators (v1)

| Capability | Why it matters | Personas |
|---|---|---|
| Fullscreen auto-pause | Detects an exclusive or windowed-fullscreen window (game, movie, browser video) and suppresses the notification; timer auto-resumes on exit. Windows Focus Assist doesn't catch windowed-fullscreen cases; every older break-reminder app is complained about on this point. | Desk Developer |

### Explicitly out of scope (v1)

- **Camera-based blink detection** — timer-based approach chosen; webcam adds permission complexity and development cost.
- **Break statistics / analytics dashboard** — user chose "notify and forget"; no data-persistence UI for v1.
- **Full-screen takeover break overlay** — user chose subtle toast; blocking the screen alienates general-wellness users.
- **Strict mode / cannot-dismiss enforcement** — niche RSI-rehab use case; adversarial to the graceful-nudge UX pattern the whole space converges on.
- **Guided eye exercise animations** — separate content domain; scope creep for a notifier.
- **macOS / Linux support** — Windows 10/11 only for v1.
- **iOS / Android companion or cross-device sync** — no account = no sync; mobile out of scope.
- **Posture reminders** — different sensing surface; would pull the product into a separate competitive set.
- **Pomodoro / productivity timer integration** — productivity concern layered on eye health; separate product.
- **Team license / team statistics** — enterprise eye health is unvalidated; defer until core product is proven.
- **In-app audio customization** — Windows toast uses OS notification sound settings; no need to own audio config.

---

## RBAC model (initial)

This is a single-user local desktop application. There is no server-side multi-tenancy.

- **User** — the person running the app on their Windows machine. Has full control: configure all
  settings (interval, schedule, startup behavior, snooze durations), start/stop the timer,
  dismiss/snooze individual notifications. There are no restrictions and no other roles for v1.

> If a team / enterprise tier is added in v2, an `admin` role would govern forced-interval policies
> pushed to enrolled machines — but that is explicitly out of scope for v1.

---

## Regulatory constraints

- **FDA SaMD wellness exclusion** — All UI copy (notification text, Store listing, GitHub README)
  must use wellness/lifestyle framing ("reminds you to blink", "helps reduce eye strain") and must
  never claim to diagnose, treat, or mitigate dry eye syndrome or any medical condition. Crossing
  into diagnostic language triggers FDA Software as a Medical Device review under 21 CFR Part 820.

- **GDPR / CCPA** — Because the app makes zero network calls and stores all state in local config
  files, no personal data is collected, processed, or transmitted. No consent banner, data subject
  rights flow, privacy notice in the UI, or data processing agreement is required for the app
  itself. (Microsoft Store submission is a separate requirement — see below.)

- **Microsoft Store submission** — Store publication requires: (a) a publicly accessible privacy
  policy URL, even if it states "no data collected"; (b) a content age rating declaration via
  Partner Center; (c) a signed MSIX package. A static `docs/privacy.md` served via GitHub Pages
  from this repo satisfies (a).

- **Windows toast accessibility (WCAG 2.1 / UIA)** — SC 1.3.3 Sensory Characteristics: the visual
  toast must be the primary notification signal; audio is supplementary and must not be the sole
  cue (relevant for hearing-impaired users). SC 4.1.2: all settings UI controls must expose
  accessible names via UI Automation / MSAA so screen readers can operate the app.

---

## Success metrics

- **Day-0 activation rate**: ≥ 90% of clean Windows 10/11 installs fire the first notification
  within 2 minutes of first launch. Measured via an automated end-to-end test on a Windows GitHub
  Actions runner that installs the MSIX, launches the app, sets a 1-minute interval, and asserts
  the toast appeared in the Windows Action Center.

- **Notification delivery reliability**: 0 missed notifications in the CI integration test suite
  across 3 consecutive intervals when the system is at idle (no fullscreen app, no DND). Measured
  by the same test harness as above, looped for 3 intervals.

- **Crash-free session rate**: ≥ 99% of sessions without a Windows exception-level crash. Measured
  via Microsoft Partner Center crash analytics, which is automatically populated for MSIX-packaged
  apps submitted to the Store.

- **Microsoft Store rating**: ≥ 4.0 ★ within the first 30 Store reviews. Observed via Partner
  Center dashboard (no instrumentation required).

- **GitHub release download velocity**: ≥ 250 downloads of the v1.0.0 GitHub release asset within
  60 days of publication. Measured via the GitHub API
  (`GET /repos/{owner}/{repo}/releases/latest` → `assets[].download_count`).

---

## Open questions for plan-system

1. **System tray countdown**: Should the tray icon display a live countdown ("18m") to the next
   notification, or just a static on/off icon? A countdown requires a background timer-tick repaint
   every minute and a custom tray icon renderer.

2. **Schedule granularity**: Should the daily schedule be (a) a single time window applied to all
   active days (simpler), or (b) per-day start/end times (covers shift workers)? This determines
   the Settings screen's data model and persistence format.

3. **Snooze options**: Should snooze offer only fixed toast-action-button durations (5 / 15 / 60
   min), or also a "custom snooze duration" input? Toast action buttons are OS-native and require
   no extra UI; custom duration needs a separate modal or settings screen.

4. **MSIX vs. portable**: Should the GitHub Releases artifact be the same signed MSIX submitted to
   the Store (one build artifact), or a separate portable .zip / single-.exe for users who avoid
   the Store? One artifact simplifies the build pipeline; two artifacts double the distribution
   surface.

5. **Privacy policy hosting**: Should the Store-required privacy policy be a static page served via
   GitHub Pages from this repo (`docs/privacy.html`), or does the user prefer to host it at a
   custom domain?
