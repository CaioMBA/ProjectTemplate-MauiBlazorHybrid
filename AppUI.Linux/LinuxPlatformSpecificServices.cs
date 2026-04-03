using AppUI.Linux;
using Domain.Interfaces.ApplicationConfigurationInterfaces;
using System.Diagnostics;
using System.Runtime.InteropServices;

[assembly: Dependency(typeof(LinuxPlatformSpecificServices))]
namespace AppUI.Linux;

public class LinuxPlatformSpecificServices : IPlatformSpecificServices
{
    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunCommandAsync(string fileName, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return (process.ExitCode, await stdOutTask, await stdErrTask);
    }

    private static (int ExitCode, string StdOut, string StdErr) RunCommand(string fileName, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdOut = process.StandardOutput.ReadToEnd();
        var stdErr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, stdOut, stdErr);
    }

    private static bool IsCommandAvailable(string commandName)
    {
        try
        {
            var result = RunCommand("which", commandName);
            return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StdOut);
        }
        catch
        {
            return false;
        }
    }

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
            if (IsCommandAvailable("zenity"))
            {
                var zenityResult = await RunCommandAsync("zenity", "--file-selection", "--directory", "--title=Select a folder");
                if (zenityResult.ExitCode == 0)
                {
                    return zenityResult.StdOut.Trim();
                }
            }

            if (IsCommandAvailable("kdialog"))
            {
                var kdialogResult = await RunCommandAsync("kdialog", "--getexistingdirectory", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                if (kdialogResult.ExitCode == 0)
                {
                    return kdialogResult.StdOut.Trim();
                }
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

            await RunCommandAsync("xdg-open", folderPath);
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
        try
        {
            if (NotifyTime > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(NotifyTime));
            }

            if (IsCommandAvailable("notify-send"))
            {
                await RunCommandAsync("notify-send", string.IsNullOrWhiteSpace(title) ? "Notification" : title, message ?? string.Empty);
                return;
            }

            Console.WriteLine($"[Linux Notification] {title}: {message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.Linux > SendLocalNotification. Error: {ex.Message}");
        }
    }
    #endregion

    #region Camera
    public async Task<string?> ScanBarcodeAsync()
    {
        try
        {
            if (IsCommandAvailable("zbarcam"))
            {
                var result = await RunCommandAsync("zbarcam", "--raw", "--oneshot", "--nodisplay");
                if (result.ExitCode == 0)
                {
                    var barcode = result.StdOut
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault();

                    return string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.Linux > ScanBarcodeAsync. Error: {ex.Message}");
        }

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
            var cpuInfoPath = "/proc/cpuinfo";
            if (File.Exists(cpuInfoPath))
            {
                foreach (var line in File.ReadLines(cpuInfoPath))
                {
                    if (line.StartsWith("model name", StringComparison.OrdinalIgnoreCase)
                        || line.StartsWith("Hardware", StringComparison.OrdinalIgnoreCase)
                        || line.StartsWith("Processor", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split(':', 2);
                        if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]))
                        {
                            return parts[1].Trim();
                        }
                    }
                }
            }

            var lscpu = RunCommand("lscpu");
            if (lscpu.ExitCode == 0)
            {
                foreach (var line in lscpu.StdOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.StartsWith("Model name:", StringComparison.OrdinalIgnoreCase))
                    {
                        return line.Replace("Model name:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                    }
                }
            }

            return RuntimeInformation.ProcessArchitecture.ToString();
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
            var memInfoPath = "/proc/meminfo";
            if (File.Exists(memInfoPath))
            {
                foreach (var line in File.ReadLines(memInfoPath))
                {
                    if (line.StartsWith("MemTotal:", StringComparison.OrdinalIgnoreCase))
                    {
                        var digits = new string(line.Where(char.IsDigit).ToArray());
                        if (long.TryParse(digits, out var kb))
                        {
                            return kb * 1024;
                        }
                    }
                }
            }
        }
        catch
        {
        }

        return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
    }

    public string GetGraphicsCard()
    {
        try
        {
            if (IsCommandAvailable("lspci"))
            {
                var lspci = RunCommand("lspci");
                if (lspci.ExitCode == 0)
                {
                    var gpuLine = lspci.StdOut
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault(l => l.Contains("VGA", StringComparison.OrdinalIgnoreCase)
                                          || l.Contains("3D controller", StringComparison.OrdinalIgnoreCase)
                                          || l.Contains("Display controller", StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrWhiteSpace(gpuLine))
                    {
                        return gpuLine.Trim();
                    }
                }
            }
        }
        catch
        {
        }

        return "Unknown Graphics Card";
    }

    public string GetOsName()
    {
        try
        {
            const string osReleasePath = "/etc/os-release";
            if (File.Exists(osReleasePath))
            {
                var prettyName = File.ReadLines(osReleasePath)
                    .FirstOrDefault(l => l.StartsWith("PRETTY_NAME=", StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(prettyName))
                {
                    return prettyName.Split('=', 2)[1].Trim().Trim('"');
                }
            }
        }
        catch
        {
        }

        return "Linux";
    }

    public string GetOsVersion()
    {
        try
        {
            const string osReleasePath = "/etc/os-release";
            if (File.Exists(osReleasePath))
            {
                var versionId = File.ReadLines(osReleasePath)
                    .FirstOrDefault(l => l.StartsWith("VERSION_ID=", StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(versionId))
                {
                    return versionId.Split('=', 2)[1].Trim().Trim('"');
                }
            }
        }
        catch
        {
        }

        return Environment.OSVersion.Version.ToString();
    }

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
