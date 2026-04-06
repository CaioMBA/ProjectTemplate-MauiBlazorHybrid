using Domain.Models.ApplicationConfigurationModels;

namespace Domain.Interfaces.ApplicationConfigurationInterfaces;

public interface IPlatformSpecificServices
{
    Task<CommandExecutionModel> RunCommand(CommandExecutionModel commandExecutionModel);
    Task OpenUrl(string Url);
    string ReadAssetContent(string path);
    Task<IEnumerable<string>> ListAssetsAsync();
    Task<string?> PickDirectory();
    Task<string?> PickFile();
    Task<IEnumerable<string>> PickFiles();
    Task OpenDirectory(string folderPath);
    Task SendLocalNotification(string title, string message, double NotifyTime = 1);
    Task<string?> ScanBarcodeAsync();
    long GetStorage(bool available = false, string? name = null);
    string GetProcessor();
    long GetRam();
    IEnumerable<string> GetGraphicsCard();
    string GetOsName();
    string GetOsVersion();
    string GetOsArchitecture();
    string GetMachineName();
    string GetUserName();
}
