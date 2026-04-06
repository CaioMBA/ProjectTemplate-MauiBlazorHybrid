using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Domain.Interfaces.ApplicationConfigurationInterfaces;
using Domain.Models.ApplicationConfigurationModels;
using Microsoft.Win32;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.ApplicationModel;
using Windows.Storage.Pickers;
using WinRT.Interop;

[assembly: Dependency(typeof(AppUI.Platforms.Windows.WindowsPlatformSpecificServices))]
namespace AppUI.Platforms.Windows;

internal class WindowsPlatformSpecificServices(IServiceProvider services) : IPlatformSpecificServices
{
    private readonly ICommandService _commandService = services.GetRequiredService<ICommandService>();
    public async Task<CommandExecutionModel> RunCommand(CommandExecutionModel commandExecutionModel)
    {
        return await _commandService.RunAsync(commandExecutionModel, (command, model) =>
        {
            var useShellExecute = model.RunAsAdministrator;
            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                UseShellExecute = useShellExecute,
                RedirectStandardOutput = !useShellExecute,
                RedirectStandardError = !useShellExecute,
                CreateNoWindow = true
            };

            if (model.RunAsAdministrator)
            {
                startInfo.Verb = "runas";
            }

            _commandService.AddArguments(startInfo, model.Parameters);
            return startInfo;
        });
    }

    public async Task OpenUrl(string Url) => await Launcher.OpenAsync(new Uri(Url));

    #region Assets
    public string ReadAssetContent(string path)
    {
        var content = string.Empty;
        var assetsPath = Package.Current.InstalledLocation.Path;
        try
        {
            var fullPath = Path.Combine(assetsPath, path);
            if (File.Exists(fullPath))
            {
                content = File.ReadAllText(fullPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.Windows > ReadAssetContent. Error: {ex.Message}");
        }
        return content;
    }

    public async Task<IEnumerable<string>> ListAssetsAsync()
    {
        var assetFiles = new ConcurrentBag<string>();
        var assetsPath = Package.Current.InstalledLocation.Path;

        if (Directory.Exists(assetsPath))
        {
            GetFilesRecursiveParallel(assetsPath, assetFiles);
        }

        return await Task.FromResult(assetFiles);
    }

    private static void GetFilesRecursiveParallel(string directory, ConcurrentBag<string> fileList)
    {
        try
        {
            string basePath = Package.Current.InstalledLocation.Path;
            string relativeDirectory = directory.Replace($"{basePath}{Path.DirectorySeparatorChar}", "");
            if (!Directory.Exists(directory))
            {
                return;
            }
            if (relativeDirectory.StartsWith(@"wwwroot\lib", StringComparison.OrdinalIgnoreCase)
                || relativeDirectory.StartsWith(@"wwwroot/lib", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }


            foreach (var file in Directory.EnumerateFiles(directory))
            {
                var relativePath = file.Replace($"{basePath}{Path.DirectorySeparatorChar}", "");
                fileList.Add(relativePath);
            }

            Parallel.ForEach(Directory.EnumerateDirectories(directory), subDir =>
            {
                GetFilesRecursiveParallel(subDir, fileList);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.Windows > GetFilesRecursiveParallel. Error: {ex.Message}");
        }
    }
    #endregion

    #region Picker
    public async Task<string?> PickDirectory()
    {
        var folderPicker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.Desktop,
            ViewMode = PickerViewMode.List
        };
        folderPicker.FileTypeFilter.Add("*");

        var hwnd = GetWindowHandle();
        if (hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to get a valid window handle.");
        }

        InitializeWithWindow.Initialize(folderPicker, hwnd);

        var folder = await folderPicker.PickSingleFolderAsync();
        return folder?.Path;
    }

    public async Task<string?> PickFile()
    {
        var filePicker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.Desktop,
            ViewMode = PickerViewMode.List,
        };
        filePicker.FileTypeFilter.Add("*");
        var hwnd = GetWindowHandle();
        if (hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to get a valid window handle.");
        }
        InitializeWithWindow.Initialize(filePicker, hwnd);
        var file = await filePicker.PickSingleFileAsync();
        return file?.Path;
    }

    public async Task<IEnumerable<string>> PickFiles()
    {
        var filePicker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.Desktop,
            ViewMode = PickerViewMode.List,
        };
        filePicker.FileTypeFilter.Add("*");
        var hwnd = GetWindowHandle();
        if (hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to get a valid window handle.");
        }
        InitializeWithWindow.Initialize(filePicker, hwnd);
        var files = await filePicker.PickMultipleFilesAsync();
        return files.Select(f => f.Path);
    }


    private static IntPtr GetWindowHandle()
    {
        var mauiWindow = Application.Current?.Windows.FirstOrDefault();
        if (mauiWindow?.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
        {
            return WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
        }

        return IntPtr.Zero;
    }

    public async Task OpenDirectory(string folderPath)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await RunCommand(new CommandExecutionModel { Commands = ["explorer.exe"], Parameters = [folderPath] });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                await RunCommand(new CommandExecutionModel { Commands = ["open"], Parameters = [folderPath] });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                await RunCommand(new CommandExecutionModel { Commands = ["xdg-open"], Parameters = [folderPath] });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.Windows > OpenDirectory. Error: {ex.Message}");
        }
    }
    #endregion

    #region Local Notifications
    public async Task SendLocalNotification(string title, string message, double NotifyTime = 1)
    {
        var duration = NotifyTime <= 5 ? ToastDuration.Short : ToastDuration.Long;
        var msg = $"{title}: {message}";
        if (string.IsNullOrEmpty(title))
        {
            msg = message;
        }

        var toast = Toast.Make(msg, duration);
        await toast.Show();
    }
    #endregion

    #region Camera
    public async Task<string?> ScanBarcodeAsync()
    {
        var scannerPage = new AppUI.Components.Pages.HandlerPages.BarcodeScanner();
        var nav = Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;
        if (nav == null)
        {
            throw new InvalidOperationException("No navigation context available.");
        }
        await nav.PushModalAsync(scannerPage);
        return await scannerPage.GetResultAsync();
    }
    #endregion

    #region SystemInfo
    public long GetStorage(bool available = false, string? name = null)
    {
        return DriveInfo.GetDrives()
            .Where(d => d.IsReady
                        && d.DriveType == DriveType.Fixed
                        && (string.IsNullOrWhiteSpace(name) || d.Name == name))
            .Sum(d => (available ? d.TotalFreeSpace : d.TotalSize));
    }

    public string GetProcessor()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            return key?.GetValue("ProcessorNameString")?.ToString() ?? "Unknown Processor";
        }
        catch
        {
            return "Unknown Processor";
        }
    }

    public long GetRam()
    {
        return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
    }

    public IEnumerable<string> GetGraphicsCard()
    {
        var names = new List<string>();

        try
        {
            var powershellResult = RunCommand(new CommandExecutionModel
            {
                Commands = ["powershell"],
                Parameters = ["-NoProfile", "-NonInteractive", "-Command", "Get-CimInstance Win32_VideoController | Select-Object -ExpandProperty Name"],
            }).GetAwaiter().GetResult();

            if (powershellResult.ExitCode == 0)
            {
                names.AddRange(powershellResult.StdOutLines
                    .Select(x => x?.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!)
                    .Distinct(StringComparer.OrdinalIgnoreCase));
            }

            string tempDxDiagPath = Path.Combine(Path.GetTempPath(), "dxdiag_output.txt");

            var dxdiagResult = RunCommand(new CommandExecutionModel
            {
                Commands = ["dxdiag"],
                Parameters = ["/t", tempDxDiagPath],
                RunAsAdministrator = false,
            }).GetAwaiter().GetResult();

            if (dxdiagResult.ExitCode == 0 && File.Exists(tempDxDiagPath))
            {
                var lines = File.ReadAllLines(tempDxDiagPath);

                names.AddRange(lines
                    .Where(line => line.TrimStart().StartsWith("Card name:", StringComparison.OrdinalIgnoreCase))
                    .Select(line => line.Split(':', 2))
                    .Where(parts => parts.Length == 2)
                    .Select(parts => parts[1].Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase));

                File.Delete(tempDxDiagPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving GPU info via dxdiag: {ex.Message}");
        }

        var result = names
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return result.Length > 0 ? result : ["Unknown Graphics Card"];
    }

    public string GetOsName()
    {
        return RuntimeInformation.OSDescription switch
        {
            string desc when desc.Contains("Windows") => "Windows",
            string desc when desc.Contains("Linux") => "Linux",
            string desc when desc.Contains("Mac OS") => "macOS",
            _ => "Unknown OS"
        };
    }

    public string GetOsVersion() => Environment.OSVersion.Version.Major.ToString();

    public string GetOsArchitecture()
    {
        return RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "x86",
            _ => "Unknown Architecture"
        };
    }

    public string GetMachineName() => Environment.MachineName;

    public string GetUserName() => Environment.UserName;
    #endregion
}
