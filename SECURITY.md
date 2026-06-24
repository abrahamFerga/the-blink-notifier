# Blink Notifier — Security

## Threat model

Blink Notifier is a local desktop wellness-reminder app. It makes no network calls, stores no credentials, and handles no sensitive user data. The attack surface is narrow.

### In scope

| Threat | Vector | Mitigation |
|---|---|---|
| Malicious settings file | Attacker writes crafted `settings.json` | Settings deserialized to a POCO with strict type annotations; `BlinkSettingsValidator` rejects out-of-range values at load time. No `eval`, no code execution from settings. |
| Privilege escalation via `runFullTrust` (MSIX) | App runs as a full-trust process | The capability is required for EventLog access and registry startup registration. The app does not expose any network ports or named pipes. It reads only its own data directories. |
| DLL hijacking (portable EXE) | Attacker plants a DLL next to the EXE | The self-contained single-file publish bundles all managed DLLs inside the host EXE. Native dependency extraction uses a per-user temp directory unique to the app, not the working directory. |
| Toast action spoofing | Attacker crafts a toast activation URI | Toast activation is handled via `ToastNotificationManagerCompat.OnActivated`; only `action=snooze&duration=N` and `action=dismiss` are processed; unrecognised actions are logged and ignored; `N` is parsed as an integer with no shell execution. |
| Log file tampering | Attacker modifies Serilog rolling log | Logs are write-only from the app's perspective and contain no secrets. Tampering affects observability only, not app behaviour. |

### Out of scope

- Network attacks (the app makes no outbound or inbound connections).
- Supply-chain attacks on NuGet packages (mitigated by package lock files and Dependabot; outside the scope of this document).
- Physical access attacks.

## Dependency vulnerabilities

The CI pipeline (`ci.yml`) runs `dotnet restore` which surfaces `NU1904` advisories for vulnerable transitive dependencies. The project explicitly pins `System.Drawing.Common` to `10.0.*` in `BlinkNotifier.Core.csproj` to override a known vulnerable transitive version from `Microsoft.Toolkit.Uwp.Notifications`.

Check for new advisories:

```powershell
dotnet list package --vulnerable --include-transitive
```

## Responsible disclosure

If you discover a security vulnerability in Blink Notifier, please **do not open a public GitHub issue**. Instead, email [abraham.fdzg@gmail.com](mailto:abraham.fdzg@gmail.com) with:

- A description of the vulnerability and its potential impact.
- Steps to reproduce.
- Any proof-of-concept code or screenshots.

You will receive an acknowledgement within 5 business days. We aim to release a fix within 30 days for confirmed issues. We will credit you in the release notes unless you prefer to remain anonymous.

Public disclosure is welcome after the fix is released, or after 90 days if no fix is forthcoming.
