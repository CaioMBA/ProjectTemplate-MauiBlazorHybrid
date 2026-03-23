using Domain;
using Domain.Enums;
using Domain.Extensions;
using Domain.Interfaces.ApplicationConfigurationInterfaces;
using Domain.Models.ApplicationConfigurationModels;

namespace Services.AuthenticationServices;

public class AccountServices(AppUtils utils, ISettingsServices settings) : IAccountServices
{
    private readonly AppUtils _utils = utils;
    private readonly ISettingsServices _settings = settings;

    public async Task<UserSessionModel?> GetUserSession()
    {
        string? userSessionJson = await _utils.GetFromSecurityStorage(SecurityStorageVariables.UserSession);
        if (!string.IsNullOrWhiteSpace(userSessionJson))
        {
            var user = userSessionJson.ToObject<UserSessionModel>()!;
            return user;
        }
        return null;
    }

    public async Task SetUserSession(UserSessionModel userSession)
    {
        await _utils.SetToSecurityStorage(SecurityStorageVariables.UserSession, userSession.ToJson());
    }

    public void RemoveUserSession()
    {
        _utils.RemoveFromSecurityStorage(SecurityStorageVariables.UserSession);
    }

    public void SetUserPreferences(UserSessionModel userSession)
    {
        if (!string.IsNullOrEmpty(userSession.Language))
        {
            _settings.ChangeLanguage(userSession.Language, true);
        }
        if (!string.IsNullOrEmpty(userSession.Theme))
        {
            _settings.ChangeTheme(userSession.Theme, true);
        }
    }
}
