using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Extensions;
using Gibbon.Git.Server.Models;

namespace Gibbon.Git.Server.Services;

public class UserNameFormatter(IHttpContextAccessor httpContextAccessor, ServerSettings serverSettings, IUserSettingsService userSettingsService)
    : IUserNameFormatter
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ServerSettings _serverSettings = serverSettings;
    private readonly IUserSettingsService _userSettingsService = userSettingsService;
    private UserSettings _userSettings;

    public string GetDisplayName(UserModel user) => Format(user, GetUserSettings().DisplayNameFormat ?? _serverSettings.DisplayNameFormat);

    public IEnumerable<UserModel> Sort(IEnumerable<UserModel> users)
    {
        var format = GetUserSettings().SortNameFormat ?? _serverSettings.SortNameFormat;

        return users
            .OrderBy(user => GetSortKey(user, format), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(user => user.Username, StringComparer.OrdinalIgnoreCase);
    }

    internal static string Format(UserModel user, NameFormat format)
    {
        if (!HasName(user))
        {
            return user.Username;
        }

        return format switch
        {
            NameFormat.FirstLast => $"{user.GivenName} {user.Surname}".Trim(),
            NameFormat.LastFirst => $"{user.Surname} {user.GivenName}".Trim(),
            _ => $"{user.Surname}, {user.GivenName}".Trim(' ', ',')
        };
    }

    internal static string GetSortKey(UserModel user, NameFormat format)
    {
        if (!HasName(user))
        {
            return user.Username;
        }

        return format == NameFormat.FirstLast
            ? $"{user.GivenName} {user.Surname}".Trim()
            : $"{user.Surname} {user.GivenName}".Trim();
    }

    private static bool HasName(UserModel user) => !string.IsNullOrWhiteSpace(user.GivenName) || !string.IsNullOrWhiteSpace(user.Surname);

    private UserSettings GetUserSettings()
    {
        if (_userSettings is not null)
        {
            return _userSettings;
        }

        var user = _httpContextAccessor.HttpContext?.User;
        _userSettings = user?.Identity?.IsAuthenticated == true
            ? _userSettingsService.GetSettings(user.Id()).GetAwaiter().GetResult()
            : _userSettingsService.GetDefaultSettings();

        return _userSettings;
    }
}
