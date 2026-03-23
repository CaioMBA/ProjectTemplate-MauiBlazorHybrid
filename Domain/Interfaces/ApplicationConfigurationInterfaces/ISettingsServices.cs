using Domain.Models.ApplicationConfigurationModels;

namespace Domain.Interfaces.ApplicationConfigurationInterfaces;

public interface ISettingsServices
{
    event Action? OnLanguageChanged;
    event Action? OnThemeChanged;

    AppLanguageModel CurrentLanguage { get; }
    AppThemeModel CurrentTheme { get; }

    IEnumerable<AppLanguageModel> AvailableLanguages();
    void ChangeLanguage(string languageCode, bool setToPreferences);

    IEnumerable<AppThemeModel> AvailableThemes();
    void ChangeTheme(string theme, bool setToPreferences);
}
