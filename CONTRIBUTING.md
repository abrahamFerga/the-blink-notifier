# Contributing to Blink Notifier

Thanks for considering a contribution. Here's what you need to know.

## Dev environment

- Windows 10 1809+ or Windows 11 (x64)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Any editor — Visual Studio 2022 17.8+, VS Code with C# Dev Kit, or Rider all work.

```powershell
git clone https://github.com/abrahamFerga/the-blink-notifier.git
cd the-blink-notifier
dotnet build --configuration Release
dotnet test --configuration Release --no-build
```

> **Note for .NET SDK 10+**: running `dotnet test` directly (build + test in one step) can
> trigger a Terminal Logger file-lock race on Windows. The two-step form above avoids it.
> CI always uses this form.

## Project layout

| Project | What it does |
|---|---|
| `src/BlinkNotifier.Settings` | Settings model, JSON persistence, validation |
| `src/BlinkNotifier.Core` | Timer, toast, fullscreen detection, snooze state |
| `src/BlinkNotifier.App` | WPF shell, tray icon, settings window, first-run wizard |
| `src/BlinkNotifier.Packaging` | MSIX manifest and Store images |
| `tests/BlinkNotifier.Settings.Tests` | Unit tests for settings |
| `tests/BlinkNotifier.Core.Tests` | Unit tests for core logic |
| `tests/BlinkNotifier.Integration.Tests` | Integration tests for toast dispatch and activation routing |

## Making changes

1. Fork the repo and create a branch from `main`.
2. Write tests for new behaviour — `dotnet test` must stay green.
3. Keep commits focused and use a short imperative subject line.
4. Open a pull request against `main` with a clear description.

## Reporting bugs

Use the [Bug report](.github/ISSUE_TEMPLATE/bug_report.yml) issue template — especially the log snippet; it saves a lot of back-and-forth.

## Code style

- C# 13, nullable enabled, implicit usings enabled — match the existing style.
- Run `dotnet format` before committing. CI enforces `dotnet format --verify-no-changes` and will fail on whitespace/style drift.
- No `Console.WriteLine` in service code; use `ILogger<T>`.
- No new external dependencies without discussion in an issue first.

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
