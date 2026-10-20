using Gibbon.Git.Server.Configuration;

namespace Gibbon.Git.Server.Data.Entities;

public sealed class ServerSettingsEntity
{
    public int Id { get; set; }

    public bool AllowAnonymousPush { get; set; }
    public bool AllowUserRepositoryCreation { get; set; }
    public bool AllowAnonymousRegistration { get; set; }
    public string DefaultLanguage { get; set; }
    public string SiteTitle { get; set; }
    public string SiteLogoUrl { get; set; }
    public string SiteCssUrl { get; set; }
    public bool IsCommitAuthorAvatarVisible { get; set; }
    public string LinksRegex { get; set; }
    public string LinksUrl { get; set; }
    public NameFormat DisplayNameFormat { get; set; }
    public NameFormat SortNameFormat { get; set; }
}
