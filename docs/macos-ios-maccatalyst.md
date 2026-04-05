# macOS + iOS + MacCatalyst Development Guide (.NET 10)

## Before you start

- If this is your own app, use a repository created from **Template** or **Fork**.
- Clone this base repository directly only when you plan to contribute with a PR.
- Run all commands from the repository root.

## Prerequisites

- macOS (recent version supported by Xcode)
- Xcode + Apple command-line tools
- .NET 10 SDK
- MAUI workload

## Install tools

Install Xcode from the App Store, then:

```bash
xcode-select --install
sudo xcodebuild -license accept
```

Install .NET 10 SDK (using Microsoft installer recommended), then verify:

```bash
dotnet --version
```

Install MAUI workload:

```bash
dotnet workload install maui
```

## Restore

```bash
dotnet restore
```

## Run targets

Run iOS:

```bash
dotnet build AppUI/AppUI.csproj -t:Run -f net10.0-ios
```

Run MacCatalyst:

```bash
dotnet build AppUI/AppUI.csproj -t:Run -f net10.0-maccatalyst
```

## If developing from Windows for iOS

Use Pair-to-Mac in Visual Studio and ensure the same .NET SDK/workloads are installed on the Mac build host.
