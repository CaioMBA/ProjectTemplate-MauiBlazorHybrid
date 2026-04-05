# Linux WSL2 (Ubuntu-based) Development Guide (.NET 10)

This guide targets Ubuntu running in WSL2 for `AppUI.Linux`.

## Before you start

- If this is your own app, use a repository created from **Template** or **Fork**.
- Clone this base repository directly only when you plan to contribute with a PR.
- Run all commands from the repository root.

## Windows prerequisites (for WSL2 scenario)

- Windows 11 with WSL2 + WSLg

## Install WSL2 + Ubuntu (PowerShell as Administrator)

```powershell
wsl --install -d Ubuntu
wsl --set-default Ubuntu
```

After reboot/login, verify:

```powershell
wsl -l -v
```

## Linux prerequisites
- Ubuntu distribution (can be WSL2 or native Linux)
- .NET 10 SDK + MAUI workload
- GTK4 + WebKitGTK dependencies


## Install .NET 10 SDK inside Linux (Ubuntu-based)

```bash
wget https://packages.microsoft.com/config/ubuntu/$(. /etc/os-release; echo $VERSION_ID)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-sdk-10.0
```

Install MAUI workload:

```bash
dotnet workload install maui
```

## Install runtime dependencies (WSL Ubuntu)

Install GTK4 and WebKit dependencies:

```bash
sudo apt install -y libgtk-4-1 libgtk-4-dev libwebkitgtk-6.0-4 libwebkitgtk-6.0-dev mesa-utils
```

## Build and run Linux host in WSL

```bash
dotnet restore
dotnet run --project AppUI.Linux/AppUI.Linux.csproj --launch-profile WSL
```

## Why `WSL` launch profile is required

The WSL profile sets software rendering and WebKit compatibility variables:

- `LIBGL_ALWAYS_SOFTWARE=1`
- `MESA_LOADER_DRIVER_OVERRIDE=llvmpipe`
- `GALLIUM_DRIVER=llvmpipe`
- `GSK_RENDERER=cairo`
- `WEBKIT_DISABLE_COMPOSITING_MODE=1`
- `WEBKIT_DISABLE_DMABUF_RENDERER=1`
