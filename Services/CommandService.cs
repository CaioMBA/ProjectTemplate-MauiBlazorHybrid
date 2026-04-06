using Domain.Interfaces.ApplicationConfigurationInterfaces;
using Domain.Models.ApplicationConfigurationModels;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Services;

public class CommandService : ICommandService
{
    public void AddArguments(ProcessStartInfo startInfo, IEnumerable<string>? arguments)
    {
        if (arguments is null)
        {
            return;
        }

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }
    }

    public async Task<CommandExecutionModel> RunAsync(
        CommandExecutionModel? commandExecutionModel,
        Func<string, CommandExecutionModel, ProcessStartInfo> startInfoFactory)
    {
        var model = commandExecutionModel ?? new CommandExecutionModel();
        model.IsRunning = true;
        model.FinishedAtUtc = null;

        if (model.Commands.Length == 0)
        {
            model.IsRunning = false;
            model.StdErrLines.Enqueue("No command was provided.");
            return model;
        }

        if (!model.WaitForExit)
        {
            _ = Task.Run(async () => await ExecuteCommandsAsync(model, startInfoFactory).ConfigureAwait(false));
            return model;
        }

        await ExecuteCommandsAsync(model, startInfoFactory).ConfigureAwait(false);
        return model;
    }

    public CommandExecutionModel BuildUnsupportedCommandExecutionModel(CommandExecutionModel? commandExecutionModel, string message)
    {
        var model = commandExecutionModel ?? new CommandExecutionModel();
        model.IsRunning = false;
        model.ExitCode = -1;
        model.FinishedAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(message))
        {
            model.StdErrLines.Enqueue(message);
        }

        return model;
    }

    private async Task ReadStreamAsync(StreamReader reader, ConcurrentQueue<string> target, IProgress<string>? progress = null)
    {
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            target.Enqueue(line);
            progress?.Report(line);
        }
    }

    private static void AddOutput(string? content, ConcurrentQueue<string> target, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        foreach (var line in content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            target.Enqueue(line);
            progress?.Report(line);
        }
    }

    private async Task ExecuteCommandsAsync(
        CommandExecutionModel model,
        Func<string, CommandExecutionModel, ProcessStartInfo> startInfoFactory)
    {
        Process? lastProcess = null;

        foreach (var command in model.Commands.Where(c => !string.IsNullOrWhiteSpace(c)))
        {
            var process = new Process
            {
                StartInfo = startInfoFactory(command, model)
            };

            process.Start();
            lastProcess = process;

            var stdOutTask = process.StartInfo.RedirectStandardOutput
                ? model.KeepReadingOutput
                    ? ReadStreamAsync(process.StandardOutput, model.StdOutLines, model.OutputProgress)
                    : Task.Run(async () => AddOutput(await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false), model.StdOutLines, model.OutputProgress))
                : Task.CompletedTask;

            var stdErrTask = process.StartInfo.RedirectStandardError
                ? model.KeepReadingOutput
                    ? ReadStreamAsync(process.StandardError, model.StdErrLines, model.ErrorProgress)
                    : Task.Run(async () => AddOutput(await process.StandardError.ReadToEndAsync().ConfigureAwait(false), model.StdErrLines, model.ErrorProgress))
                : Task.CompletedTask;

            await process.WaitForExitAsync().ConfigureAwait(false);
            await Task.WhenAll(stdOutTask, stdErrTask).ConfigureAwait(false);

            model.ExitCode = process.ExitCode;
            process.Dispose();

            if (model.ExitCode != 0)
            {
                break;
            }
        }

        model.IsRunning = false;
        model.FinishedAtUtc = DateTime.UtcNow;
        if (lastProcess is null)
        {
            model.StdErrLines.Enqueue("No valid command was provided.");
        }
    }
}
