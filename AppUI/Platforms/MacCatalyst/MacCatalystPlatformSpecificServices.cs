using CommunityToolkit.Maui.Storage;
using Domain.Interfaces.ApplicationConfigurationInterfaces;
using Domain.Models.ApplicationConfigurationModels;
using Foundation;
using Metal;
using ObjCRuntime;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AppleOption;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UIKit;

[assembly: Dependency(typeof(AppUI.Platforms.MacCatalyst.MacCatalystPlatformSpecificServices))]
namespace AppUI.Platforms.MacCatalyst;

internal class MacCatalystPlatformSpecificServices(IServiceProvider services) : IPlatformSpecificServices
{
    private readonly ICommandService _commandService = services.GetRequiredService<ICommandService>();

    public Task<CommandExecutionModel> RunCommand(CommandExecutionModel commandExecutionModel)
    {
        return _commandService.RunAsync(commandExecutionModel, (command, model) =>
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = model.RunAsAdministrator ? "sudo" : command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (model.RunAsAdministrator)
            {
                startInfo.ArgumentList.Add(command);
            }

            _commandService.AddArguments(startInfo, model.Parameters);
            return startInfo;
        });
    }

    public async Task OpenUrl(string Url) => await Launcher.OpenAsync(new Uri(Url));

    #region Assets
    public string ReadAssetContent(string path)
    {
        string content = string.Empty;
        var bundlePath = NSBundle.MainBundle.BundlePath;
        try
        {
            var fullPath = $"{bundlePath}/{path}";
            if (File.Exists(fullPath))
            {
                content = File.ReadAllText(fullPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.MacCatalyst > ReadAssetContent. Error: {ex.Message}");
        }
        return content;
    }

    public async Task<IEnumerable<string>> ListAssetsAsync()
    {
        List<string> assetFiles = new();
        var bundlePath = NSBundle.MainBundle.BundlePath;

        if (Directory.Exists(bundlePath))
        {
            GetFilesRecursive(bundlePath, assetFiles);
        }

        return await Task.FromResult(assetFiles);
    }

    private void GetFilesRecursive(string directory, List<string> fileList)
    {
        try
        {
            var files = Directory.GetFiles(directory);
            foreach (var file in files)
            {
                var relativePath = file.Replace($"{NSBundle.MainBundle.BundlePath}/", "");
                fileList.Add(relativePath);
            }

            var subDirectories = Directory.GetDirectories(directory);
            foreach (var subDir in subDirectories)
            {
                GetFilesRecursive(subDir, fileList);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.MacCatalyst > GetFilesRecursive. Error: {ex.Message}");
        }
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

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.MacCatalyst > PickDirectory. Error: {ex.Message}");
            return null;
        }
    }

    public async Task<string?> PickFile()
    {
        try
        {
            var options = new PickOptions
            {
                PickerTitle = "Please select a file",
            };
            var result = await FilePicker.PickAsync(options);
            if (result is null)
            {
                return null;
            }
            return result.FullPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.MacCatalyst > PickFile. Error: {ex.Message}");
            return null;
        }
    }

    public async Task<IEnumerable<string>> PickFiles()
    {
        List<string?> filePaths = [];
        try
        {
            var options = new PickOptions
            {
                PickerTitle = "Please select files",
            };
            var results = await FilePicker.PickMultipleAsync(options);
            if (results != null)
            {
                filePaths.AddRange(results.Select(r => r?.FullPath));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.MacCatalyst > PickFiles. Error: {ex.Message}");
        }
        return filePaths.Where(x => !string.IsNullOrEmpty(x))!;
    }

    public async Task OpenDirectory(string folderPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                return;
            }

            var uri = new Uri($"file://{folderPath}");
            await Launcher.OpenAsync(uri);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.MacCatalyst > OpenDirectory. Error: {ex.Message}");
        }
    }
    #endregion

    #region Local Notifications
    public async Task SendLocalNotification(string title, string message, double NotifyTime = 1)
    {
        var notification = new NotificationRequest
        {
            NotificationId = new Random().Next(int.MinValue, int.MaxValue),
            Title = title,
            Description = message,
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = DateTime.Now.AddSeconds(NotifyTime)
            },
            Apple = new AppleOptions
            {
                Priority = ApplePriority.Critical,
                ApplyBadgeValue = true,
                PresentAsBanner = true,
                ShowInNotificationCenter = true
            }
        };

        await LocalNotificationCenter.Current.Show(notification);
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
        try
        {
            NSFileSystemAttributes? attributes = NSFileManager.DefaultManager.GetFileSystemAttributes(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal)
            );

            long? storage = available ? (long?)attributes?.FreeSize : (long?)attributes?.Size;
            if (storage == null)
            {
                Console.WriteLine("Failed to retrieve storage information.");
                return 0;
            }

            return (long)storage;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetStorage Error] Failed to retrieve storage information: {ex}");
            return 0;
        }
    }

    public string GetProcessor()
    {
        return "Apple Silicon / Intel";
    }

    public long GetRam()
    {
        return (long)NSProcessInfo.ProcessInfo.PhysicalMemory;
    }

    public IEnumerable<string> GetGraphicsCard()
    {
        try
        {
            var name = MTLDevice.SystemDefault?.Name;
            return !string.IsNullOrWhiteSpace(name) ? [name] : ["Apple GPU"];
        }
        catch
        {
            return ["Apple GPU"];
        }
    }

    public string GetOsName() => "MacCatalyst";

    public string GetOsVersion() => NSProcessInfo.ProcessInfo.OperatingSystemVersionString;

    public string GetOsArchitecture()
    {
        return RuntimeInformation.ProcessArchitecture.ToString();
    }

    public string GetMachineName()
    {
        return Environment.MachineName;
    }

    public string GetUserName()
    {
        return Environment.UserName;
    }
    #endregion
}
