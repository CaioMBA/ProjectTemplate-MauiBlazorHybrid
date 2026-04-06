using AppUI.Linux;
using Domain.Interfaces.ApplicationConfigurationInterfaces;
using Domain.Models.ApplicationConfigurationModels;
using System.Diagnostics;
using System.Runtime.InteropServices;

[assembly: Dependency(typeof(LinuxPlatformSpecificServices))]
namespace AppUI.Linux;

internal class LinuxPlatformSpecificServices(IServiceProvider services) : IPlatformSpecificServices
{
    private readonly ICommandService _commandService = services.GetRequiredService<ICommandService>();

    public async Task<CommandExecutionModel> RunCommand(CommandExecutionModel commandExecutionModel)
    {
        return await _commandService.RunAsync(commandExecutionModel, (command, model) =>
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

    private (int ExitCode, string StdOut, string StdErr) RunCommandSync(string fileName, params string[] arguments)
    {
        var model = RunCommand(new CommandExecutionModel { Commands = [fileName], Parameters = arguments }).GetAwaiter().GetResult();
        return (model.ExitCode, model.StdOut, model.StdErr);
    }

    private bool IsCommandAvailable(string commandName)
    {
        try
        {
            var result = RunCommandSync("which", commandName);
            return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StdOut);
        }
        catch
        {
            return false;
        }
    }

    public async Task OpenUrl(string Url)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Url))
            {
                return;
            }
            await RunCommand(new CommandExecutionModel { Commands = ["xdg-open"], Parameters = [Url] });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.Linux > OpenUrl. Error: {ex.Message}");
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
    public async Task<string?> PickFile()
    {
        try
        {
            if (IsCommandAvailable("zenity"))
            {
                var zenityResult = await RunCommand(new CommandExecutionModel { Commands = ["zenity"], Parameters = ["--file-selection", "--title=Select a file"] });
                if (zenityResult.ExitCode == 0)
                {
                    return zenityResult.StdOut.Trim();
                }
            }
            if (IsCommandAvailable("kdialog"))
            {
                var kdialogResult = await RunCommand(new CommandExecutionModel { Commands = ["kdialog"], Parameters = ["--getopenfilename", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)] });
                if (kdialogResult.ExitCode == 0)
                {
                    return kdialogResult.StdOut.Trim();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.Linux > PickFile. Error: {ex.Message}");
        }
        return null;
    }

    public async Task<IEnumerable<string>> PickFiles()
    {
        var files = new List<string>();
        try
        {
            if (IsCommandAvailable("zenity"))
            {
                var zenityResult = await RunCommand(new CommandExecutionModel { Commands = ["zenity"], Parameters = ["--file-selection", "--multiple", "--title=Select files"] });
                if (zenityResult.ExitCode == 0)
                {
                    files.AddRange(zenityResult.StdOut.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim()));
                }
            }
            else if (IsCommandAvailable("kdialog"))
            {
                var kdialogResult = await RunCommand(new CommandExecutionModel { Commands = ["kdialog"], Parameters = ["--getopenfilename", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "--multiple"] });
                if (kdialogResult.ExitCode == 0)
                {
                    files.AddRange(kdialogResult.StdOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim()));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on AppUI.Platforms.Linux > PickFiles. Error: {ex.Message}");
        }
        return files;
    }

    public async Task<string?> PickDirectory()
    {
        try
        {
            if (IsCommandAvailable("zenity"))
            {
                var zenityResult = await RunCommand(new CommandExecutionModel { Commands = ["zenity"], Parameters = ["--file-selection", "--directory", "--title=Select a folder"] });
                if (zenityResult.ExitCode == 0)
                {
                    return zenityResult.StdOut.Trim();
                }
            }

            if (IsCommandAvailable("kdialog"))
            {
                var kdialogResult = await RunCommand(new CommandExecutionModel { Commands = ["kdialog"], Parameters = ["--getexistingdirectory", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)] });
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

            await RunCommand(new CommandExecutionModel { Commands = ["xdg-open"], Parameters = [folderPath] });
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
                await RunCommand(new CommandExecutionModel { Commands = ["notify-send"], Parameters = [string.IsNullOrWhiteSpace(title) ? "Notification" : title, message ?? string.Empty] });
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
                var result = await RunCommand(new CommandExecutionModel { Commands = ["zbarcam"], Parameters = ["--raw", "--oneshot", "--nodisplay"] });
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

            var lscpu = RunCommandSync("lscpu");
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

    public IEnumerable<string> GetGraphicsCard()
    {
        var names = new List<string>();

        try
        {
            if (IsCommandAvailable("lspci"))
            {
                var lspci = RunCommandSync("lspci");
                if (lspci.ExitCode == 0)
                {
                    var gpuLines = lspci.StdOut
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(l => l.Contains("VGA", StringComparison.OrdinalIgnoreCase)
                                 || l.Contains("3D controller", StringComparison.OrdinalIgnoreCase)
                                 || l.Contains("Display controller", StringComparison.OrdinalIgnoreCase));

                    foreach (var gpuLine in gpuLines)
                    {
                        var cleaned = gpuLine.Split(':', 3).LastOrDefault()?.Trim();
                        if (!string.IsNullOrWhiteSpace(cleaned))
                        {
                            var revIndex = cleaned.IndexOf(" (rev", StringComparison.OrdinalIgnoreCase);
                            names.Add(revIndex > 0 ? cleaned[..revIndex].Trim() : cleaned);
                        }
                    }
                }
            }

            if (IsCommandAvailable("glxinfo"))
            {
                var glxInfo = RunCommandSync("glxinfo", "-B");
                if (glxInfo.ExitCode == 0)
                {
                    var deviceLine = glxInfo.StdOut
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault(l => l.TrimStart().StartsWith("Device:", StringComparison.OrdinalIgnoreCase)
                                          || l.TrimStart().StartsWith("OpenGL renderer string:", StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrWhiteSpace(deviceLine))
                    {
                        var cleaned = deviceLine.Split(':', 2).LastOrDefault()?.Trim();
                        if (!string.IsNullOrWhiteSpace(cleaned))
                        {
                            names.Add(cleaned);
                        }
                    }
                }
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
