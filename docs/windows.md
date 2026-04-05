# Windows Development Guide (.NET 10)

## Before you start

- If this is your own app, use a repository created from **Template** or **Fork**.
- Clone this base repository directly only when you plan to contribute with a PR.
- Run all commands from the repository root.

## Prerequisites

- Windows 10/11
- Visual Studio Enterprise 2026 (18.4.x) with:
  - .NET MAUI workload

## Install .NET 10 SDK (PowerShell)

```powershell
winget install Microsoft.DotNet.SDK.10
```

Verify:

```powershell
dotnet --version
```

## Install MAUI workload

```powershell
dotnet workload install maui
```

## Build and run

Restore:

```powershell
dotnet restore
```

Run Windows target:

```powershell
dotnet build AppUI/AppUI.csproj -t:Run -f net10.0-windows10.0.19041.0
```

## Troubleshooting

- If workloads are inconsistent:

```powershell
dotnet workload repair
```

- If targets are not visible, run a workload update:

```powershell
dotnet workload update
```
