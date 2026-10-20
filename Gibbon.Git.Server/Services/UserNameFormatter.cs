using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Models;

namespace Gibbon.Git.Server.Services;

public class UserNameFormatter(ServerSettings serverSettings, CurrentUserSettings currentUserSettings)
    : IUserNameFormatter
{
    private readonly ServerSettings _serverSettings = serverSettings;
    private readonly CurrentUserSettings _currentUserSettings = currentUserSettings;

    public string GetDisplayName(UserModel user) => Format(user, _currentUserSettings.Value.DisplayNameFormat ?? _serverSettings.DisplayNameFormat);

    public IEnumerable<UserModel> Sort(IEnumerable<UserModel> users)
    {
        var format = _currentUserSettings.Value.SortNameFormat ?? _serverSettings.SortNameFormat;

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
}
