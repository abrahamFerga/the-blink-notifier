# Industry research: eye-health

## Top commercial players

1. **LookAway** (Mystical Bits) — Mac-first intelligent break scheduler combining smart context-aware timing, blink rate reminders, and posture alerts; cross-device iPhone sync; one-time purchase. Founded ~2023. Customer count unknown; described by reviewers as "the most polished app in this category." Segment: prosumer / SMB team.

2. **BlinkEasy** — Commercial webcam-based blink rate monitor for Windows and macOS; tracks blink frequency in real time and fires alerts when the rate drops below threshold; generates printable blink-rate reports; 100% offline. Founded unknown. Free 14-day trial then paid. Segment: prosumer.

3. **ScreenBlink** — Free open-source desktop blink tracker (Katun Li) that uses real-time camera detection plus optional timer-based reminders; includes Pomodoro integration and fullscreen-game support; ARM-Mac + Windows. Founded ~2024. Customer count unknown. Segment: prosumer (free tier of the camera-based space).

4. **Workrave** — Open-source RSI prevention suite (active since 2001, GPLv3) with configurable microbreaks, guided eye exercises, strict-mode enforcement, daily usage limits, and multi-period break statistics; Windows and Linux primary, macOS port in progress. 204 AlternativeTo likes — highest in the space. Segment: developer / office worker prosumer (free).

5. **Eye Care 20 20 20** — iOS timer app implementing the 20-20-20 rule with scheduled work-hour reminders, no account required, and optional subscription premium. 4.3 ★ / 379 App Store reviews. Segment: consumer (freemium).

---

## Capability matrix

| Capability | LookAway | BlinkEasy | ScreenBlink | Workrave | Eye Care 20 20 20 |
|---|---|---|---|---|---|
| Timer-based break reminders | ✓ deep | ✓ basic | ✓ basic | ✓ deep | ✓ deep |
| Camera-based blink detection | — | ✓ deep | ✓ deep | — | — |
| Blink rate low-threshold alert | ✓ basic | ✓ deep | ✓ deep | — | — |
| Blink analytics / printable reports | — | ✓ deep | ✓ basic | — | — |
| Break statistics (daily/weekly) | ✓ basic | ✓ deep | ✓ basic | ✓ deep | — |
| Visual popup / overlay reminders | ✓ deep | ✓ deep | ✓ deep | ✓ deep | ✓ basic |
| Full-screen break takeover | ✓ deep | — | — | ✓ deep | — |
| Audio alerts | ✓ deep | — | ✓ deep | ✓ basic | ✓ basic |
| Custom notification style (position, color, text) | ✓ deep | ✓ deep | ✓ deep | ✓ basic | — |
| Snooze / postpone controls | ✓ deep | — | — | ✓ deep | ✓ basic |
| Strict mode (cannot skip breaks) | — | — | — | ✓ deep | — |
| Custom break frequency | ✓ deep | ✓ deep | ✓ deep | ✓ deep | ✓ basic |
| Custom break duration | ✓ deep | ✓ deep | ✓ deep | ✓ deep | — |
| Guided eye exercises (animated) | — | — | ✓ basic | ✓ deep | — |
| 20-20-20 rule content / instructions | ✓ deep | ✓ deep | ✓ deep | ✓ basic | ✓ deep |
| Auto-pause during fullscreen / gaming | ✓ deep | — | ✓ deep | — | — |
| Auto-pause during meetings / screen recording | ✓ deep | — | — | — | — |
| Smart break timing (waits for natural pause) | ✓ deep | — | — | — | — |
| Schedule-based activation (work hours only) | ✓ deep | — | — | ✓ deep | ✓ deep |
| Posture reminders | ✓ basic | — | — | ✓ basic | — |
| Daily computer usage limit enforcement | — | — | — | ✓ deep | — |
| Cross-device sync (desktop + mobile) | ✓ deep | — | — | — | — |
| Keyboard shortcuts | ✓ basic | — | ✓ deep | ✓ basic | — |
| Pomodoro timer integration | — | — | ✓ basic | — | — |
| Menu bar / system tray integration | ✓ deep | — | ✓ basic | ✓ deep | — |
| 100 % offline / local processing | — | ✓ deep | ✓ deep | ✓ deep | ✓ basic |
| No account required | ✓ basic | ✓ deep | ✓ deep | ✓ deep | ✓ deep |
| Team license / multi-seat | ✓ deep | — | — | — | — |
| Windows support | — | ✓ deep | ✓ deep | ✓ deep | — |
| macOS support | ✓ deep | ✓ deep | ✓ basic | — | — |
| Linux support | — | — | — | ✓ deep | — |
| iOS / mobile companion | ✓ basic | — | — | — | ✓ deep |

---

## Synthesized capabilities

### Must-have (v1)
Capabilities present in at least 4 of 5 players — table-stakes for any entrant.

- **Timer-based break reminders** — fire a notification at a configurable interval (default: every 20 minutes per the 20-20-20 rule); without this the app has no core loop.
- **Visual popup / overlay reminders** — a visible on-screen prompt the user cannot miss; every player ships this.
- **20-20-20 rule content** — brief instructional copy telling the user what to do during the break (look 20 ft away for 20 s); sets the health context.
- **Custom break frequency** — users vary from 10-minute to 60-minute cycles; a hard-coded interval kills adoption.
- **No account required** — zero sign-up friction; all 5 players agree on this.
- **100 % offline / local processing** — 4/5 players make this a selling point; privacy is a salient concern when a webcam is involved.
- **Break statistics (daily/weekly)** — 4/5 players surface at least a simple streak or count; gives the user a feedback loop.

### Differentiator (v1)
Capabilities present in 1–2 players with high impact; choose at most one to anchor positioning.

- **Camera-based real-time blink detection** — only BlinkEasy and ScreenBlink; the only way to know the user *actually* needs to blink rather than just estimating it's time. High-effort implementation but the clearest differentiation from pure timer apps. Exemplified by BlinkEasy.
- **Smart context-aware auto-pause** — only LookAway; suppresses reminders during fullscreen video, meetings, or deep-focus windows so notifications don't interrupt. Exemplified by LookAway.

### Skip for v1
Naming what's out is as load-bearing as naming what's in.

- **Guided eye exercise animations** — scope creep for a notifier; users wanting exercises have Workrave. Adds content-production burden with no clear user demand from first-time users.
- **Strict mode / cannot-skip enforcement** — niche; acceptable for RSI rehab programs (Workrave's use case), alienating for general wellness. Ship opt-in snooze instead.
- **Daily computer usage limits / keyboard restriction** — Workrave-specific RSI tooling; far outside "blink notifier" scope.
- **Posture reminders** — different sensing / UX surface from blink tracking; would pull the product into a different competitive set (posture wearables, Ergonofis apps).
- **Linux support** — only Workrave has traction there; add after v1 if the user base requests it.
- **Team license / team statistics** — LookAway is proving this market exists, but enterprise eye health is a harder sell than consumer; skip until core product is validated.
- **Pomodoro integration** — productivity feature layered on top; a separate concern from eye health.
- **iOS native app** — high platform investment; deliver a good desktop experience first.

---

## Notable UX patterns observed

- **Menu bar / system tray as the universal anchor** — every desktop player surfaces its only persistent UI here; no dedicated always-visible window. Users expect zero visual footprint between reminders. Seen in: LookAway, ScreenBlink, Workrave.
- **Countdown during breaks** — break screens universally show a countdown timer (e.g. "20 seconds remaining") rather than a dismiss button; the countdown is the UX signal that the break has an end. Seen in: LookAway, Workrave, Eye Care 20 20 20.
- **Graceful-skip over hard-block** — outside Workrave's strict mode, no player hard-blocks the keyboard/mouse; they offer snooze or dismiss instead. Enforcement causes resentment; friction-free nudges improve adherence. Seen in: LookAway, ScreenBlink, BlinkEasy.
- **Fullscreen compatibility** — newer apps explicitly call out "works during fullscreen games and video" as a feature, suggesting it is a frequent complaint about older break-reminder apps. Seen in: ScreenBlink, LookAway.
- **Privacy-first messaging** — camera-based apps all lead with "100 % offline / no data leaves your device" prominently on their homepage; this is a mandatory trust signal when webcam access is requested. Seen in: BlinkEasy, ScreenBlink.

---

## Compliance / regulatory considerations

- **Camera access consent (platform-mandated)** — macOS requires an explicit privacy permission prompt before any app accesses the camera (`NSCameraUsageDescription` Info.plist key); Windows requires a similar consent flow since Win 10. Failure to handle denial gracefully crashes or confuses users. Not a regulation but a hard platform requirement.
- **GDPR / CCPA (if any telemetry)** — if the app collects crash reports, analytics, or cloud-synced stats, EU and California users require consent + data minimization + a privacy policy. All camera-based players avoid this by going 100 % offline; follow that pattern unless cloud sync is a deliberate product decision.
- **FDA Software as a Medical Device (SaMD)** — an app that claims to *diagnose, treat, or mitigate* a medical condition (e.g. "treats dry eye syndrome") may fall under FDA SaMD regulation (21 CFR Part 820). Stay firmly in the wellness/lifestyle framing ("reminds you to blink") and avoid diagnostic language to remain out of scope.
- **ADA / WCAG 2.1 accessibility** — notifications must have non-audio equivalents for users with hearing impairments (visual flash or overlay); screen-reader compatibility for settings UI.

---

## Open questions for the user

1. **Blink detection approach**: Should v1 use camera-based real-time blink detection (like BlinkEasy/ScreenBlink) or timer-based 20-20-20 reminders (like Workrave/Eye Care)? Camera-based is more accurate but requires webcam permission and more development effort. Timer-based ships faster. Or both, selectable?

2. **Target platforms at v1**: The current machine is Windows 11. Should v1 be Windows-only, or also macOS? (Linux can wait per skip list.)

3. **Notification style**: Should breaks use a subtle popup the user can dismiss (like ScreenBlink), or a full-screen overlay that demands attention (like Workrave/LookAway)? This is a core UX decision that shapes the whole notification system.

4. **Analytics / reporting**: Should the app track and display blink-rate history or break adherence stats, or is a simple "notify and forget" sufficient for v1?

5. **Distribution model**: Personal/local tool (no installer, just a binary) or a distributable app (Microsoft Store / GitHub releases)? This affects packaging, signing, and update story.
