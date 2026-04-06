using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using CommunityToolkit.Maui.Storage;
using Domain.Interfaces.ApplicationConfigurationInterfaces;
using Domain.Models.ApplicationConfigurationModels;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using Environment = Android.OS.Environment;

[assembly: Dependency(typeof(AppUI.Platforms.Android.AndroidPlatformSpecificServices))]
namespace AppUI.Platforms.Android;

internal class AndroidPlatformSpecificServices(IServiceProvider services) : IPlatformSpecificServices
{
    private readonly ICommandService _commandService = services.GetRequiredService<ICommandService>();

    public Task<CommandExecutionModel> RunCommand(CommandExecutionModel commandExecutionModel)
    {
        return Task.FromResult(_commandService.BuildUnsupportedCommandExecutionModel(commandExecutionModel, "Terminal command execution is not supported on Android in this service."));
    }

    public async Task OpenUrl(string Url) => await Launcher.OpenAsync(new Uri(Url));

    #region Assets
    public string ReadAssetContent(string path)
    {
        string content = string.Empty;
        AssetManager? assets = Platform.AppContext.Assets;
        try
        {
            using Stream? stream = assets.Open(path);
            using StreamReader? reader = new(stream);
            content = reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.Android > ReadAssetContent. Error: {ex.Message}");
        }
        return content;
    }

    public async Task<IEnumerable<string>> ListAssetsAsync()
    {
        List<string> assetFiles = new();
        AssetManager? assets = Platform.AppContext.Assets;

        ListAssetsRecursive(assets, "", assetFiles);

        return await Task.FromResult(assetFiles);
    }

    private void ListAssetsRecursive(AssetManager? assets, string path, List<string> fileList)
    {
        try
        {
            string[]? files = assets?.List(path);
            if (files != null)
            {
                foreach (var file in files)
                {
                    string fullPath = string.IsNullOrEmpty(path) ? file : $"{path}/{file}";

                    string[]? subFiles = assets?.List(fullPath);
                    if (subFiles?.Length > 0)
                    {
                        ListAssetsRecursive(assets, fullPath, fileList);
                    }
                    else
                    {
                        fileList.Add(fullPath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.Android > ListAssetsRecursive. Error: {ex.Message}");
        }
    }
    #endregion

    #region Picker
    public async Task<string?> PickFile()
    {
        try
        {
            var options = new PickOptions
            {
                PickerTitle = "Please select a file",
            };
            FileResult? result = await FilePicker.PickAsync(options);
            return result?.FullPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.Android > PickFile. Error: {ex.Message}");
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
            Console.WriteLine($"Error on AppUI.Platforms.iOS > PickFiles. Error: {ex.Message}");
        }
        return filePaths.Where(x => !string.IsNullOrEmpty(x))!;
    }

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
            Console.WriteLine($"Error on AppUI.Platforms.Android > PickDirectory. Error: {ex.Message}");
            return null;
        }
    }

    public async Task OpenDirectory(string folderPath)
    {
        try
        {
            var file = new Java.IO.File(folderPath);
            var uri = FileProvider.GetUriForFile(Platform.CurrentActivity, $"{Platform.CurrentActivity.PackageName}.fileprovider", file);

            var intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(uri, "*/*");
            intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.GrantReadUriPermission);

            Platform.CurrentActivity.StartActivity(intent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.Android > OpenDirectory. Error: {ex.Message}");
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
            Android = new AndroidOptions
            {
                ChannelId = "default",
                Priority = AndroidPriority.High,
                AutoCancel = true,
                Ongoing = false
            }
        };

        await LocalNotificationCenter.Current.Show(notification);
    }
    #endregion

    #region Camera
    public async Task<string?> ScanBarcodeAsync()
    {
        var scannerPage = new AppUI.Components.Pages.HandlerPages.BarcodeScanner();
        var nav = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;
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
            var statFs = new StatFs(Environment.DataDirectory?.AbsolutePath);
            return available ? statFs.AvailableBytes : statFs.TotalBytes;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error getting storage: {ex.Message}");
            return 0;
        }
    }

    public string GetProcessor()
    {
        try
        {
            return Java.Lang.JavaSystem.GetProperty("os.arch") ?? "Unknown Processor";
        }
        catch
        {
            return "Unknown Processor";
        }
    }

    public long GetRam()
    {
        try
        {
            var activityManager = (ActivityManager)Platform.AppContext.GetSystemService(Context.ActivityService);
            var memoryInfo = new ActivityManager.MemoryInfo();
            activityManager?.GetMemoryInfo(memoryInfo);
            return memoryInfo.TotalMem;
        }
        catch
        {
            return 0;
        }
    }

    private static string? TryGetProp(string key)
    {
        try
        {
            using var process = new Java.Lang.ProcessBuilder("/system/bin/getprop", key)
                .RedirectErrorStream(true)
                .Start();

            using var reader = new Java.IO.BufferedReader(new Java.IO.InputStreamReader(process.InputStream));
            var value = reader.ReadLine()?.Trim();
            process.WaitFor();

            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch
        {
            return null;
        }
    }

    public IEnumerable<string> GetGraphicsCard()
    {
        var names = new List<string>();

        try
        {
            string[] properties = ["ro.hardware.egl", "ro.hardware.vulkan", "ro.board.platform"];

            foreach (var property in properties)
            {
                var value = TryGetProp(property);
                if (!string.IsNullOrWhiteSpace(value)
                    && !value.Equals("unknown", StringComparison.OrdinalIgnoreCase)
                    && !value.Equals("none", StringComparison.OrdinalIgnoreCase))
                {
                    names.Add(value);
                }
            }

            if (!string.IsNullOrWhiteSpace(Build.Hardware))
            {
                names.Add(Build.Hardware);
            }

            if (!string.IsNullOrWhiteSpace(Build.Board))
            {
                names.Add(Build.Board);
            }
        }
        catch
        {
        }

        var result = names
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return result.Length > 0 ? result : ["Unknown Graphics Card"];
    }

    public string GetOsName() => "Android";

    public string GetOsVersion() => Build.VERSION.Release ?? "Unknown";

    public string GetOsArchitecture()
    {
        return Build.SupportedAbis?.FirstOrDefault() ?? "Unknown";
    }

    public string GetMachineName()
    {
        return Build.Model ?? "Unknown Device";
    }

    public string GetUserName()
    {
        return "Android User"; // No user concept like Windows
    }
    #endregion
}
