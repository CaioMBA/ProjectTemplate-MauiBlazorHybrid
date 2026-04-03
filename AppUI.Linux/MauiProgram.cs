using CrossCutting;
using AppUI.States.ViewStates;
using Domain.Interfaces.ApplicationConfigurationInterfaces;
using Domain.Interfaces.StateInterfaces;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using Platform.Maui.Linux.Gtk4.BlazorWebView;
using Platform.Maui.Linux.Gtk4.Essentials.Hosting;
using Platform.Maui.Linux.Gtk4.Hosting;

namespace AppUI.Linux
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp
                .CreateBuilder()
                .UseMauiAppLinuxGtk4<App>()
                .AddLinuxGtk4Essentials()
                .ConfigureMauiHandlers(handlers =>
                {
                    handlers.AddHandler<BlazorWebView, Platform.Maui.Linux.Gtk4.BlazorWebView.BlazorWebViewHandler>();
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddLinuxGtk4BlazorWebView();

            builder.Services
            .AddAppSettings()
            .AddSingleton<IPlatformSpecificServices, LinuxPlatformSpecificServices>()
            .AddSingleton<IRefreshViewState, RefreshViewState>()
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
}
