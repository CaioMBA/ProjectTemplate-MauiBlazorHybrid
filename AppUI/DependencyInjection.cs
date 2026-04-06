using AppUI.States.ViewStates;
using CommunityToolkit.Maui;
using CrossCutting;
using Domain.Extensions;
using Domain.Interfaces.ApplicationConfigurationInterfaces;
using Domain.Interfaces.StateInterfaces;
using Domain.Models.ApplicationConfigurationModels;
using Microsoft.Extensions.Configuration;

namespace AppUI;

public static class DependencyInjection
{
    public static IServiceCollection AddAppSettings(this IServiceCollection serviceCollection)
    {
        using var stream = OpenAppSettingsStream();

        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        serviceCollection.AddSingleton(configuration);

        return serviceCollection.AddConfiguration(configuration);
    }

    private static Stream OpenAppSettingsStream()
    {
        string appSettingsFileName = "appsettings.json";
        try
        {
            return FileSystem.OpenAppPackageFileAsync(appSettingsFileName).GetAwaiter().GetResult();
        }
        catch
        {
            string[] candidatePaths =
            [
                Path.Combine(AppContext.BaseDirectory, appSettingsFileName),
                Path.Combine(AppContext.BaseDirectory, "Resources", appSettingsFileName),
                Path.Combine(AppContext.BaseDirectory, "Resources", "Raw", appSettingsFileName)
            ];

            foreach (var path in candidatePaths)
            {
                if (File.Exists(path))
                {
                    return File.OpenRead(path);
                }
            }
        }

        throw new FileNotFoundException("Unable to locate appsettings.json in MAUI package or Linux output paths.");
    }

    public static IServiceCollection AddPlatformServiceDependencies(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddMauiBlazorWebView();

        return serviceCollection
#if ANDROID
            .AddSingleton<IPlatformSpecificServices, Platforms.Android.AndroidPlatformSpecificServices>()
#elif IOS
        .AddSingleton<IPlatformSpecificServices, AppUI.Platforms.iOS.IosPlatformSpecificServices>()
#elif MACCATALYST
        .AddSingleton<IPlatformSpecificServices, AppUI.Platforms.MacCatalyst.MacCatalystPlatformSpecificServices>()
#elif WINDOWS
        .AddSingleton<IPlatformSpecificServices, AppUI.Platforms.Windows.WindowsPlatformSpecificServices>()
#endif
            .AddSingleton<IRefreshViewState, RefreshViewState>();
    }

    public static async Task<IServiceCollection> AddAssets(this IServiceCollection serviceCollection)
    {
        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var assetService = serviceProvider.GetRequiredService<IPlatformSpecificServices>();
        var languageDictionaryMapping = serviceProvider.GetRequiredService<ILanguageDictionaryMapping>();
        IEnumerable<string> assets = await assetService.ListAssetsAsync();

        IEnumerable<AppLanguageModel?> languages = assets.Where(x => (x.Trim().StartsWith("languages", StringComparison.OrdinalIgnoreCase)
                                                                        || x.Trim().StartsWith(@"resources/raw/languages", StringComparison.OrdinalIgnoreCase)
                                                                        || x.Trim().StartsWith(@"resources\raw\languages", StringComparison.OrdinalIgnoreCase))
                                                                      && x.Trim().EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                                                            .Select(file =>
                                                            {
                                                                string content = assetService.ReadAssetContent(file);
                                                                content = languageDictionaryMapping.ReplaceTokens(content);
                                                                var languageModel = content.ToObject<AppLanguageModel>();
                                                                if (languageModel != null)
                                                                {
                                                                    string fileName = Path.GetFileNameWithoutExtension(file).ToLower();
                                                                    languageModel.Code = fileName;
                                                                    languageModel.Flag = $"{fileName}.svg";
                                                                    return languageModel;
                                                                }
                                                                return null;
                                                            }).Distinct();

        IEnumerable<AppThemeModel?> themes = assets.Where(x =>
                                                (x.Trim().StartsWith(@"wwwroot/css/themes", StringComparison.OrdinalIgnoreCase)
                                                 ||
                                                 x.Trim().StartsWith(@"wwwroot\css\themes", StringComparison.OrdinalIgnoreCase))
                                                && x.Trim().EndsWith(@".css", StringComparison.OrdinalIgnoreCase))
                                                .Select(x =>
                                                {
                                                    var name = x
                                                              .Replace(@"wwwroot/css/themes/", "", StringComparison.OrdinalIgnoreCase)
                                                              .Replace(@"wwwroot\css\themes\", "", StringComparison.OrdinalIgnoreCase)
                                                              .Replace(@".css", "", StringComparison.OrdinalIgnoreCase);

                                                    return new AppThemeModel()
                                                    {
                                                        Name = name,
                                                        Path = x.ToLower(),
                                                        Theme = name.ToAppTheme(),
                                                        Icon = $"{name}_theme.png"
                                                    };
                                                })
                                                .Distinct();

        return serviceCollection.AddStaticFiles(themes.Where(x => x != null)!, languages.Where(x => x != null)!);
    }
}
