using Domain;
using Domain.Enums;
using Domain.Extensions;
using Domain.Interfaces.ApplicationConfigurationInterfaces;
using Domain.Models.ApplicationConfigurationModels;
using System.Globalization;

namespace Services;

public class SettingsServices : ISettingsServices
{
    private readonly AppUtils _utils;
    private readonly IEnumerable<AppLanguageModel> _availableLanguages;
    private readonly IEnumerable<AppThemeModel> _availableThemes;

    public event Action? OnLanguageChanged;
    public event Action? OnThemeChanged;

    public AppLanguageModel CurrentLanguage { get; private set; }
    public AppThemeModel CurrentTheme { get; private set; }

    public SettingsServices(
        AppUtils utils,
        IEnumerable<AppLanguageModel> availableLanguages,
        IEnumerable<AppThemeModel> availableThemes)
    {
        _utils = utils;
        _availableLanguages = availableLanguages;
        _availableThemes = availableThemes;

        CurrentLanguage = GetStartLanguage();
        CurrentTheme = GetStartTheme();
    }

    #region Language
    public IEnumerable<AppLanguageModel> AvailableLanguages() => _availableLanguages;

    private AppLanguageModel GetStartLanguage()
    {
        var preferenceLanguage = _utils.GetFromPreferences(PreferenceVariables.Language);
        if (!String.IsNullOrWhiteSpace(preferenceLanguage))
        {
            return preferenceLanguage.ToObject<AppLanguageModel>()
                ?? throw new InvalidCastException("It was not possible to serialize the language configuration");
        }

        return _availableLanguages
            .FirstOrDefault(x => x.Code!.Equals(CultureInfo.CurrentCulture.Name, StringComparison.OrdinalIgnoreCase))
            ?? _availableLanguages.FirstOrDefault()
            ?? throw new InvalidOperationException("No languages were configured to this application");
    }

    public void ChangeLanguage(string languageCode, bool setToPreferences)
    {
        var newLanguage = _availableLanguages.FirstOrDefault(x => x.Code!.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
        if (newLanguage is null || newLanguage == CurrentLanguage)
        {
            return;
        }

        CurrentLanguage = newLanguage;
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(languageCode);
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(languageCode);
        if (setToPreferences)
        {
            _utils.SetToPreferences(PreferenceVariables.Language, newLanguage.ToJson());
        }

        OnLanguageChanged?.Invoke();
    }
    #endregion Language


    #region Theme
    private AppThemeModel GetStartTheme()
    {
        var preferenceTheme = _utils.GetFromPreferences(PreferenceVariables.Theme);
        if (!String.IsNullOrWhiteSpace(preferenceTheme))
        {
            return preferenceTheme.ToObject<AppThemeModel>()
                ?? throw new InvalidCastException("It was not possible to serialize the theme configuration");
        }

        return _availableThemes.FirstOrDefault(x => x.Theme == _utils.GetSystemTheme())
            ?? _availableThemes.FirstOrDefault()
            ?? throw new InvalidOperationException("No themes were configured to this application");
    }

    public IEnumerable<AppThemeModel> AvailableThemes() => _availableThemes;

    public void ChangeTheme(string theme, bool setToPreferences)
    {
        var newTheme = _availableThemes.FirstOrDefault(x => x.Name!.Equals(theme, StringComparison.OrdinalIgnoreCase));
        if (newTheme is null || newTheme == CurrentTheme)
        {
            return;
        }

        CurrentTheme = newTheme;
        if (setToPreferences)
        {
            _utils.SetToPreferences(PreferenceVariables.Theme, newTheme.ToJson());
        }

        OnThemeChanged?.Invoke();
    }
    #endregion Skin
}

