using Domain.Interfaces.ApplicationConfigurationInterfaces;
using Domain.Models.ApplicationConfigurationModels;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Services.AuthenticationServices;

public class CustomAuthenticationStateProvider(
    IAccountServices accountServices,
    IOptionsMonitor<AppSettingsModel> options) : AuthenticationStateProvider
{
    private readonly AppSettingsModel _settings = options.CurrentValue;
    private readonly IAccountServices _accountServices = accountServices;
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
    private UserSessionModel? _currentUserSession;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var authenticationState = new AuthenticationState(_anonymous);

        var UserSession = await CurrentUserSession();
        if (UserSession is not null)
        {
            _currentUserSession = UserSession;
            var identity = GetClaimsPrincipal(UserSession);
            var user = new ClaimsPrincipal(identity);
            authenticationState = new AuthenticationState(user);
        }
        return authenticationState;
    }

    public async Task UpdateAuthenticationState(UserSessionModel? userSession, bool rememberUser = false)
    {
        ClaimsPrincipal claimsPrincipal = _anonymous;

        if (userSession is not null)
        {
            _currentUserSession = userSession;
            if (rememberUser)
            {
                await _accountServices.SetUserSession(userSession);
            }
            claimsPrincipal = GetClaimsPrincipal(userSession);
            _accountServices.SetUserPreferences(userSession);
        }
        else
        {
            _currentUserSession = null;
            _accountServices.RemoveUserSession();
        }
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
    }

    public async Task<UserSessionModel?> CurrentUserSession()
    {
        return _currentUserSession ?? await _accountServices.GetUserSession();
    }

    private ClaimsPrincipal GetClaimsPrincipal(UserSessionModel userSession)
    {
        var claimList = new List<Claim>()
        {
            new (ClaimTypes.Name, userSession.Name),
            new (ClaimTypes.Email, userSession.Email),
        };

        foreach (var role in userSession.Roles)
        {
            claimList.Add(new Claim(ClaimTypes.Role, role));
        }

        var claims = new ClaimsIdentity(claimList, _settings.AppName);
        var identity = new ClaimsIdentity(claims);
        return new ClaimsPrincipal(identity);
    }
}
