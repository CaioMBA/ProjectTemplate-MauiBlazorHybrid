using Domain.Models.ApplicationConfigurationModels;
using System.Diagnostics;

namespace Domain.Interfaces.ApplicationConfigurationInterfaces;

public interface ICommandService
{
    Task<CommandExecutionModel> RunAsync(
        CommandExecutionModel? commandExecutionModel,
        Func<string, CommandExecutionModel, ProcessStartInfo> startInfoFactory);

    CommandExecutionModel BuildUnsupportedCommandExecutionModel(
        CommandExecutionModel? commandExecutionModel,
        string message);

    void AddArguments(ProcessStartInfo startInfo, IEnumerable<string>? arguments);
}
