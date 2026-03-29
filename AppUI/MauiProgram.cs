using AppUI.States.ViewStates;
using Camera.MAUI;
using CommunityToolkit.Maui;
using CrossCutting;
using Domain.Extensions;
using Domain.Interfaces.ApplicationConfigurationInterfaces;
using Domain.Interfaces.StateInterfaces;
using Domain.Models.ApplicationConfigurationModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
#if NET10_0 && !ANDROID && !IOS && !MACCATALYST && !WINDOWS
using Platform.Maui.Linux.Gtk4.BlazorWebView;
using Platform.Maui.Linux.Gtk4.Essentials.Hosting;
using Platform.Maui.Linux.Gtk4.Hosting;
#endif


namespace AppUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder.AddPlatformBuilderDependencies();

        builder.Services
            .AddAppSettings()
            .AddPlatformServiceDependencies()
            .AddUtilities()
            .AddAssets().GetAwaiter().GetResult()
            .AddHttpClients()
            .AddDatabaseClients().GetAwaiter().GetResult()
            .AddServices()
            .AddAppAuthentication();


#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static IServiceCollection AddAppSettings(this IServiceCollection serviceCollection)
    {
        using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").GetAwaiter().GetResult();

        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        serviceCollection.AddSingleton(configuration);

        return serviceCollection.AddConfiguration(configuration);
    }

    private static MauiAppBuilder AddPlatformBuilderDependencies(this MauiAppBuilder builder)
    {
        return builder
#if NET10_0 && !ANDROID && !IOS && !MACCATALYST && !WINDOWS
            .UseMauiAppLinuxGtk4<App>()
            .AddLinuxGtk4Essentials()
#else
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCameraView()
            .UseLocalNotification()
#endif
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });
    }

    private static IServiceCollection AddPlatformServiceDependencies(this IServiceCollection serviceCollection)
    {
#if NET10_0 && !ANDROID && !IOS && !MACCATALYST && !WINDOWS
        serviceCollection.AddLinuxGtk4BlazorWebView();
#else
        serviceCollection.AddMauiBlazorWebView();
#endif

        return serviceCollection
#if ANDROID
            .AddSingleton<IPlatformSpecificServices, Platforms.Android.AndroidPlatformSpecificServices>()
#elif IOS
            .AddSingleton<IPlatformSpecificServices, AppUI.Platforms.iOS.IosPlatformSpecificServices>()
#elif WINDOWS
            .AddSingleton<IPlatformSpecificServices, AppUI.Platforms.Windows.WindowsPlatformSpecificServices>()
#elif NET10_0 && !ANDROID && !IOS && !MACCATALYST && !WINDOWS
            .AddSingleton<IPlatformSpecificServices, AppUI.Platforms.Linux.LinuxPlatformSpecificServices>()
#endif
            .AddSingleton<IRefreshViewState, RefreshViewState>();
    }

    private static async Task<IServiceCollection> AddAssets(this IServiceCollection serviceCollection)
    {
        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var assetService = serviceProvider.GetRequiredService<IPlatformSpecificServices>();
        var languageDictionaryMapping = serviceProvider.GetRequiredService<ILanguageDictionaryMapping>();
        IEnumerable<string> assets = await assetService.ListAssetsAsync();

        IEnumerable<AppLanguageModel?> languages = assets.Where(x => x.Trim().StartsWith("language", StringComparison.OrdinalIgnoreCase)
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
