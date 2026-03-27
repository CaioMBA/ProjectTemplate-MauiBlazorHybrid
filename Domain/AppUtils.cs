using Domain.Enums;
using Domain.Extensions;
using Domain.Interfaces.ApplicationConfigurationInterfaces;
using Domain.Models.ApplicationConfigurationModels;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Domain;

public class AppUtils(
    IOptionsMonitor<AppSettingsModel> options,
    IPlatformSpecificServices platformService,
    IDistributedCache cache,
    ILogger<AppUtils> logger)
{
    private readonly AppSettingsModel _appSettings = options.CurrentValue;
    private readonly IPlatformSpecificServices _platformService = platformService;
    private readonly ILogger<AppUtils> _logger = logger;

    public AppSettingsModel GetSettings() => _appSettings;

    #region Connection Helpers
    public DataBaseConnectionModel GetDataBase(string DataBaseID)
    {
        DataBaseConnectionModel? Conection = (from v in _appSettings.DataBaseConnectionModels
                                              where v.DataBaseID.Equals(DataBaseID, StringComparison.OrdinalIgnoreCase)
                                              select v).FirstOrDefault();

        return Conection ?? throw new InvalidOperationException($"Database not found using Id: {DataBaseID}");
    }

    public ApiConnectionModel GetApi(string ApiID)
    {
        ApiConnectionModel? Conection = (from v in _appSettings.ApiConnections
                                         where v.ApiID.Equals(ApiID, StringComparison.OrdinalIgnoreCase)
                                         select v).FirstOrDefault();

        return Conection ?? throw new InvalidOperationException($"API not found using Id: {ApiID}");
    }

    public ApiEndPointConnectionModel GetApiEndpoint(string ApiID, string EndpointID)
    {
        ApiConnectionModel Api = GetApi(ApiID);

        ApiEndPointConnectionModel? Endpoint = (from v in Api.EndPoints
                                                where v.EndPointID.Equals(EndpointID, StringComparison.OrdinalIgnoreCase)
                                                select v).FirstOrDefault();

        return Endpoint ?? throw new InvalidOperationException($"API endpoint not found using Id: {Endpoint}");

    }

    public ApiEndPointConnectionModel GetApiEndpoint(ApiConnectionModel Api, string EndpointID)
    {
        ApiEndPointConnectionModel? Endpoint = (from v in Api.EndPoints
                                                where v.EndPointID.Equals(EndpointID, StringComparison.OrdinalIgnoreCase)
                                                select v).FirstOrDefault();

        return Endpoint ?? throw new InvalidOperationException($"API endpoint not found using Id: {Endpoint}");

    }
    #endregion Connection Helpers

    #region Security Storage
    public async Task<string?> GetFromSecurityStorage(SecurityStorageVariables Enum)
    {
        var cryptedValue = await SecureStorage.Default.GetAsync(Enum.ToString());
        if (string.IsNullOrEmpty(cryptedValue))
        {
            return null;
        }

        try
        {
            return cryptedValue.Decrypt();
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"Error decoding Base64: {ex.Message}");
            return null;
        }
        catch (CryptographicException ex)
        {
            Console.WriteLine($"Error during decryption: {ex.Message}");
            return null;
        }
    }

    public async Task SetToSecurityStorage(SecurityStorageVariables Enum, string Value)
    {
        string cryptedValue = Value.Encrypt();
        await SecureStorage.Default.SetAsync(Enum.ToString(), cryptedValue);
    }

    public void RemoveFromSecurityStorage(SecurityStorageVariables Enum)
    {
        SecureStorage.Default.Remove(Enum.ToString());
    }

    public void ClearSecurityStorage()
    {
        SecureStorage.Default.RemoveAll();
    }
    #endregion Security Storage

    #region Preferences
    public string? GetFromPreferences(PreferenceVariables Enum)
    {
        var cryptedValue = Preferences.Get(Enum.ToString(), null);
        if (string.IsNullOrEmpty(cryptedValue))
        {
            return null;
        }
        try
        {
            return cryptedValue.Decrypt();
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"Error decoding Base64: {ex.Message}");
            return null;
        }
        catch (CryptographicException ex)
        {
            Console.WriteLine($"Error during decryption: {ex.Message}");
            return null;
        }
    }

    public void SetToPreferences(PreferenceVariables Enum, string Value)
    {
        string cryptedValue = Value.Encrypt();
        Preferences.Set(Enum.ToString(), cryptedValue);
    }

    public void RemoveFromPreferences(PreferenceVariables Enum)
    {
        Preferences.Remove(Enum.ToString());
    }

    public void ClearPreferences()
    {
        Preferences.Clear();
    }
    #endregion Preferences

    #region Cache
    public async Task SetToCache(string key, object value, TimeSpan? absoluteExpirationRelativeToNow = null)
    {
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow ?? TimeSpan.FromHours(1)
        };
        await cache.SetAsync(key, value.ToBytes().Compress(), cacheOptions);
    }

    public async Task<T?> GetFromCache<T>(string key)
    {
        var cachedData = await cache.GetAsync(key);
        if (cachedData == null)
        {
            return default;
        }
        return cachedData.Decompress().ToObject<T>();
    }
    #endregion Cache

    public string GetSystemFilePath() => FileSystem.AppDataDirectory;

    public AppTheme GetSystemTheme() => AppInfo.Current.RequestedTheme;

    public async Task OpenUrl(string Url) => await Launcher.OpenAsync(new Uri(Url));

    public async Task OpenDirectory(string folderPath) => await _platformService.OpenDirectory(folderPath);

    public async Task<string?> PickDirectory() => await _platformService.PickDirectory();

    public async Task<FileResult?> PickFileResult()
    {
        var options = new PickOptions
        {
            PickerTitle = "Please select a file",
        };
        return await FilePicker.PickAsync(options);
    }

    public async Task SendLocalNotification(string title, string message) => await _platformService.SendLocalNotification(title, message);

    public async Task<string?> ScanBarcode() => await _platformService.ScanBarcodeAsync();
}
