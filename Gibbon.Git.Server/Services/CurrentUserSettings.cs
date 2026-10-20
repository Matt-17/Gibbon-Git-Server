using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Extensions;

namespace Gibbon.Git.Server.Services;

public class CurrentUserSettings(IHttpContextAccessor httpContextAccessor, IUserSettingsService userSettingsService)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IUserSettingsService _userSettingsService = userSettingsService;
    private UserSettings _settings;

    public UserSettings Value => _settings ??= Load();

    private UserSettings Load()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        return user?.Identity?.IsAuthenticated == true
            ? _userSettingsService.GetSettings(user.Id()).GetAwaiter().GetResult()
            : _userSettingsService.GetDefaultSettings();
    }
}
