using Camera.MAUI;
using CommunityToolkit.Maui;
using CrossCutting;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;


namespace AppUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCameraView()
            .UseLocalNotification()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

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
}
