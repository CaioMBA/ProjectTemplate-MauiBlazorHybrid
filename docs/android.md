# Android Development Guide (.NET 10)

## Before you start

- If this is your own app, use a repository created from **Template** or **Fork**.
- Clone this base repository directly only when you plan to contribute with a PR.
- Run all commands from the repository root.

## Prerequisites

- Windows, macOS, or Linux
- .NET 10 SDK
- MAUI workload
- Android SDK + emulator/device tooling
- Visual Studio Enterprise 2026 (18.4.3) or equivalent MAUI-capable IDE

## Install .NET 10 SDK

Windows (PowerShell):

```powershell
winget install Microsoft.DotNet.SDK.10
```

macOS/Linux: install from Microsoft .NET installers/packages for your distro/OS.

Verify:

```bash
dotnet --version
```

## Install MAUI workload

```bash
dotnet workload install maui
```

## Android setup

1. Install Android SDK, platform tools, and emulator tooling from your IDE.
2. Create and start an Android emulator, or connect a physical Android device.

## Build and run

Restore:

```bash
dotnet restore
```

Run Android:

```bash
dotnet build AppUI/AppUI.csproj -t:Run -f net10.0-android
```

Run Android on a specific device:

```bash
dotnet build AppUI/AppUI.csproj -t:Run -f net10.0-android -p:AndroidDeviceId=<device-id>
```

## Troubleshooting

Repair workloads:

```bash
dotnet workload repair
```

Update workloads:

```bash
dotnet workload update
```
