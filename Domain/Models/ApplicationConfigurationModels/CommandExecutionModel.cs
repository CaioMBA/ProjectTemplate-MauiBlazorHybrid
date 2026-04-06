using System.Collections.Concurrent;

namespace Domain.Models.ApplicationConfigurationModels;

public class CommandExecutionModel
{
    public string[] Commands { get; init; } = [];
    public string[] Parameters { get; init; } = [];
    public bool RunAsAdministrator { get; init; }
    public bool KeepReadingOutput { get; init; }
    public bool WaitForExit { get; init; } = true;
    public IProgress<string>? OutputProgress { get; init; }
    public IProgress<string>? ErrorProgress { get; init; }
    public bool IsRunning { get; set; }
    public int ExitCode { get; set; } = -1;
    public DateTime StartedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? FinishedAtUtc { get; set; }
    public ConcurrentQueue<string> StdOutLines { get; } = new();
    public ConcurrentQueue<string> StdErrLines { get; } = new();

    public string StdOut => string.Join(Environment.NewLine, StdOutLines);
    public string StdErr => string.Join(Environment.NewLine, StdErrLines);
}
