# ProjectTemplate-MauiBlazorHybrid

Production-ready `.NET MAUI Blazor Hybrid` template for multi-platform apps with shared architecture and optional Linux desktop host.

## How to use this repository (important)

If your goal is to create your own app, **do not clone this repository directly as your project base**.

Use this decision guide:

| Goal | Recommended action |
|---|---|
| Start your own app from this base | **Use GitHub "Use this template"** |
| Keep a copy under your account and optionally sync upstream | **Fork** |
| Help improve this base for everyone | **Clone and open a PR to this repository** |

### Why

- Using a template/fork keeps ownership and history clean for your app.
- Cloning this base directly is best only when you plan to contribute changes back.

## Compatibility

This repository currently supports:

| Platform | Project | Target | Host OS needed | Status |
|---|---|---|---|---|
| Android | `AppUI` | `net10.0-android` | Windows/macOS/Linux | ✅ |
| iOS | `AppUI` | `net10.0-ios` | macOS (or paired Mac) | ✅ |
| macOS (MacCatalyst) | `AppUI` | `net10.0-maccatalyst` | macOS | ✅ |
| Windows | `AppUI` | `net10.0-windows10.0.19041.0` | Windows 10/11 | ✅ |
| Linux Desktop (GTK4) | `AppUI.Linux` | `net10.0` | Native Linux / WSL2 | ✅ |

## What is included

- Clean layered architecture: `AppUI`, `Domain`, `Services`, `Data`, `CrossCutting`
- Shared Blazor Hybrid UI with native MAUI host
- Native Linux desktop entry project (`AppUI.Linux`) based on GTK4 + BlazorWebView
- Launch profiles for Linux scenarios:
  - `WSL` profile with software-rendering compatibility variables
  - `Linux` profile for native Linux execution defaults
- Centralized framework setup via `Directory.Build.props` (`net10.0`, nullable, implicit usings)
- XAML source generation enabled (`MauiXamlInflator=SourceGen`) for better build/runtime behavior

## Project structure

- `AppUI`: main MAUI Blazor Hybrid app
- `AppUI.Linux`: Linux desktop host for the shared app
- `Domain`: entities/models/contracts/core abstractions
- `Services`: application and business services
- `Data`: persistence/integration implementations
- `CrossCutting`: dependency injection and shared wiring

## Quick start (no surprises)

1. Create your repository from this project using **Template** (recommended) or **Fork**.
2. Clone **your own** repository locally.
3. Install `.NET SDK 10`.
4. Install MAUI workload:

```bash
dotnet workload install maui
```

5. Restore packages from repository root:

```bash
dotnet restore
```

6. Rename solution/projects/namespaces to your product naming.
7. Update app metadata in `AppUI/AppUI.csproj`:
   - `ApplicationTitle`
   - `ApplicationId`
   - version fields
8. Configure app settings in `AppUI/Resources/Raw/appsettings.json`.
9. Choose startup project based on platform:
   - `AppUI` for Android/iOS/MacCatalyst/Windows
   - `AppUI.Linux` for Linux desktop

## Required environment by platform

### Common (all platforms)

- `.NET SDK 10`
- MAUI workload installed

### Windows

- Windows 10/11
- Visual Studio 2026 with MAUI tooling

### Android

- Windows, macOS, or Linux
- MAUI workload installed
- Android SDK + emulator/device
- Visual Studio 2026 (Windows) or equivalent MAUI-capable IDE

### iOS + MacCatalyst

- macOS with Xcode and Apple command-line tools
- MAUI workload installed on Mac
- For development from Windows, use Pair-to-Mac

### Linux desktop (`AppUI.Linux`)

- Linux distro with GTK4 and WebKitGTK runtime available
- For WSL2, use `WSL` profile from `AppUI.Linux/Properties/launchSettings.json`
- For native Linux, use `Linux` profile

## Run commands

Run commands from the repository root.

### MAUI project (`AppUI`)

Windows:

```bash
dotnet build AppUI/AppUI.csproj -t:Run -f net10.0-windows10.0.19041.0
```

Android:

```bash
dotnet build AppUI/AppUI.csproj -t:Run -f net10.0-android
```

iOS (Mac required):

```bash
dotnet build AppUI/AppUI.csproj -t:Run -f net10.0-ios
```

MacCatalyst (Mac required):

```bash
dotnet build AppUI/AppUI.csproj -t:Run -f net10.0-maccatalyst
```

### Linux project (`AppUI.Linux`)

```bash
dotnet run --project AppUI.Linux/AppUI.Linux.csproj
```

### Build validation

```bash
dotnet build
```

## Contributing to this base template

If you cloned this repository directly and want improvements here for everyone, open a PR against this repository.

## Additional documentation

Platform-specific setup guides:

- `docs/windows.md`
- `docs/android.md`
- `docs/macos-ios-maccatalyst.md`
- `docs/linux-ubuntu.md`
