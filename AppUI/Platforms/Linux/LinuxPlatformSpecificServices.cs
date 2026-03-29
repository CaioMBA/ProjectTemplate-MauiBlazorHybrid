using CommunityToolkit.Maui.Storage;
using Domain.Interfaces.ApplicationConfigurationInterfaces;
using System.Diagnostics;
using System.Runtime.InteropServices;

[assembly: Dependency(typeof(AppUI.Platforms.Linux.LinuxPlatformSpecificServices))]
namespace AppUI.Platforms.Linux;

public class LinuxPlatformSpecificServices : IPlatformSpecificServices
{
    #region Assets
    public string ReadAssetContent(string path)
    {
        var content = string.Empty;
        var assetsPath = AppContext.BaseDirectory;

        try
        {
            var normalizedPath = path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(assetsPath, normalizedPath);
            if (File.Exists(fullPath))
            {
                content = File.ReadAllText(fullPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.Linux > ReadAssetContent. Error: {ex.Message}");
        }

        return content;
    }

    public async Task<IEnumerable<string>> ListAssetsAsync()
    {
        var assetsPath = AppContext.BaseDirectory;
        var assetFiles = new List<string>();

        try
        {
            if (!Directory.Exists(assetsPath))
            {
                return assetFiles;
            }

            foreach (var file in Directory.EnumerateFiles(assetsPath, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(assetsPath, file).Replace(Path.DirectorySeparatorChar, '/');
                if (relative.StartsWith("wwwroot/lib", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                assetFiles.Add(relative);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.Linux > ListAssetsAsync. Error: {ex.Message}");
        }

        return await Task.FromResult(assetFiles);
    }
    #endregion

    #region Picker
    public async Task<string?> PickDirectory()
    {
        try
        {
            FolderPickerResult? folder = await FolderPicker.PickAsync(default);
            if (folder == null)
            {
                return null;
            }

            if (folder.IsSuccessful)
            {
                return folder.Folder.Path;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.Linux > PickDirectory. Error: {ex.Message}");
        }

        return null;
    }

    public async Task OpenDirectory(string folderPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                return;
            }

            await Task.Run(() =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = folderPath,
                    UseShellExecute = true
                });
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.Linux > OpenDirectory. Error: {ex.Message}");
        }
    }
    #endregion

    #region Local Notifications
    public async Task SendLocalNotification(string title, string message, double NotifyTime = 1)
    {
        await Task.CompletedTask;
        Console.WriteLine($"[Linux Notification] {title}: {message}");
    }
    #endregion

    #region Camera
    public async Task<string?> ScanBarcodeAsync()
    {
        await Task.CompletedTask;
        return null;
    }
    #endregion

    #region SystemInfo
    public long GetStorage(bool available = false, string? name = null)
    {
        return DriveInfo.GetDrives()
            .Where(d => d.IsReady
                        && d.DriveType == DriveType.Fixed
                        && (string.IsNullOrWhiteSpace(name) || d.Name == name))
            .Sum(d => available ? d.AvailableFreeSpace : d.TotalSize);
    }

    public string GetProcessor()
    {
        try
        {
            return RuntimeInformation.ProcessArchitecture.ToString();
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

    public string GetGraphicsCard()
    {
        return "Linux GPU";
    }

    public string GetOsName() => "Linux";

    public string GetOsVersion() => Environment.OSVersion.Version.ToString();

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
