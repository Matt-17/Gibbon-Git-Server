namespace Gibbon.Git.Server.Configuration;

public sealed class UserSettings
{
    public string PreferredLanguage { get; set; }
    public NameFormat? DisplayNameFormat { get; set; }
    public NameFormat? SortNameFormat { get; set; }
    public DateFormat? DateFormat { get; set; }
    public TimeFormat? TimeFormat { get; set; }

}
