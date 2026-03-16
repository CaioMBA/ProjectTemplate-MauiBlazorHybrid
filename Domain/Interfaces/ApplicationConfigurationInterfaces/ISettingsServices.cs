using Domain.Models.ApplicationConfigurationModels;

namespace Domain.Interfaces.ApplicationConfigurationInterfaces;

public interface ISettingsServices
{
    event Action? OnLanguageChanged;
    event Action? OnThemeChanged;

    AppLanguageModel CurrentLanguage { get; }
    AppThemeModel CurrentTheme { get; }

    List<AppLanguageModel> AvailableLanguages();
    void ChangeLanguage(string languageCode, bool setToPreferences);

    List<AppThemeModel> AvailableThemes();
    void ChangeTheme(string theme, bool setToPreferences);
}
