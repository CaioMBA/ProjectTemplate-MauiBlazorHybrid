# ProjectTemplate-MauiBlazorHybrid

A production-ready **.NET MAUI Blazor Hybrid template** for building multi-platform apps from a single codebase.

This template is designed to help you quickly fork, customize, and ship applications for:

- `Android`
- `iOS`
- `macOS` (via `MacCatalyst`)
- `Windows`

## Purpose

Use this repository as a starting point for multi-platform apps that need:

- Shared domain and service layers
- Blazor UI inside a native MAUI host
- Dependency injection and configuration setup
- Cross-platform platform-specific services
- A scalable structure (`AppUI`, `Domain`, `Services`, `Data`, `CrossCutting`)

## Project structure

- `AppUI` → .NET MAUI Blazor Hybrid host app (startup project)
- `Domain` → models, enums, contracts, mappings, core utilities
- `Services` → business/application services
- `Data` → database|api access and persistence concerns
- `CrossCutting` → shared DI/extensions/infrastructure wiring

## Prerequisites

- `.NET SDK 10`
- MAUI workloads installed
- IDE:
  - `Visual Studio 2026` (Windows) with MAUI/mobile workloads, or
  - `Visual Studio for Mac`/`JetBrains Rider` with MAUI support
- Platform-specific requirements:
  - `Android` emulator/device + SDK
  - `iOS/MacCatalyst` requires a Mac build host and Apple tooling
  - `Windows` development requires Windows 10/11

Install MAUI workloads (if needed):

```bash
dotnet workload install maui
```

## Getting started

1. Fork this repository (or click **Use this template**) and clone your copy.
2. Restore dependencies:

```bash
dotnet restore
```

3. Open the solution in your IDE.
4. Set `AppUI` as startup project.
5. Choose target framework/device and run.

---

## All ways to run the app

## 1) Visual Studio (recommended)

1. Open the solution.
2. Set `AppUI` as startup project.
3. In the run target selector, choose one target:
   - `net10.0-android` (emulator/device)
   - `net10.0-ios` (simulator/device, Mac required)
   - `net10.0-maccatalyst` (Mac required)
   - `net10.0-windows10.0.19041.0`
4. Press `F5` (debug) or `Ctrl+F5` (run without debugging).

## 2) .NET CLI - Windows target

Run on Windows:

```bash
dotnet build AppUI/AppUI.csproj -t:Run -f net10.0-windows10.0.19041.0
```

## 3) .NET CLI - Android target

Run on Android emulator/device:

```bash
dotnet build AppUI/AppUI.csproj -t:Run -f net10.0-android
```

If multiple devices are available, specify one:

```bash
dotnet build AppUI/AppUI.csproj -t:Run -f net10.0-android -p:AndroidDeviceId=<device-id>
```

## 4) .NET CLI - iOS target (Mac required)

```bash
dotnet build AppUI/AppUI.csproj -t:Run -f net10.0-ios
```

## 5) .NET CLI - MacCatalyst target (Mac required)

```bash
dotnet build AppUI/AppUI.csproj -t:Run -f net10.0-maccatalyst
```

## 6) Build-only (all configured target frameworks on current OS)

Useful to validate setup without launching the app:

```bash
dotnet build AppUI/AppUI.csproj
```

## Configuration

- Main app config file: `AppUI/Resources/Raw/appsettings.json`
- Update API/database placeholders before running real integrations.

## Using this template for your own app

- Rename solution/projects/namespaces to your product naming.
- Replace app metadata in `AppUI/AppUI.csproj` (`ApplicationTitle`, `ApplicationId`).
- Add your pages/components/services while keeping shared layers isolated.
- Keep platform-specific code in `AppUI/Platforms/*`.

## Notes

- This template targets `.NET 10`.
- On non-Windows OS, Windows target is naturally excluded.
- On Linux, only Android target is configured in the project by default.
