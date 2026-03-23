using Domain.Interfaces.StateInterfaces;
using Domain.Models.ApplicationConfigurationModels;
using Microsoft.Extensions.Options;

namespace AppUI;

public partial class App : Application
{
    private readonly AppSettingsModel _appSettings;
    private readonly IRefreshViewState _refreshViewState;
    public App(IOptionsMonitor<AppSettingsModel> options, IRefreshViewState refreshViewState)
    {
        _appSettings = options.CurrentValue;
        _refreshViewState = refreshViewState;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage(_refreshViewState)) { Title = _appSettings.AppName };
    }
}
