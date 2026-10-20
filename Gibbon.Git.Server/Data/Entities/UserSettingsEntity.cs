using Gibbon.Git.Server.Configuration;

namespace Gibbon.Git.Server.Data.Entities;

public class UserSettingsEntity
{
    public int UserId { get; set; }
    public string PreferredLanguage { get; set; }
    public ThemeMode PreferredThemeMode { get; set; }
    public bool ReceiveEmailNotifications { get; set; }
    public string TimeZone { get; set; }
    public string DateFormat { get; set; }
    public string DefaultHomePage { get; set; }
    public NameFormat? DisplayNameFormat { get; set; }
    public NameFormat? SortNameFormat { get; set; }
}
