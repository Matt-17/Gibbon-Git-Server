using System.ComponentModel.DataAnnotations;
using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Middleware.Attributes;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Gibbon.Git.Server.Models;

public class ServerSettingsModel
{
    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_AllowAnonymousPush")]
    public bool AllowAnonymousPush { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_AllowAnonymousRegistration")]
    public bool AllowAnonymousRegistration { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_AllowUserRepositoryCreation")]
    public bool AllowUserRepositoryCreation { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_DefaultLanguage")]
    public string DefaultLanguage { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_DisplayNameFormat")]
    public NameFormat DisplayNameFormat { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_SortNameFormat")]
    public NameFormat SortNameFormat { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_DateFormat")]
    public DateFormat DateFormat { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_TimeFormat")]
    public TimeFormat TimeFormat { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_DefaultRepositoryView")]
    public RepositoryView DefaultRepositoryView { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_CloneTools")]
    public List<string> EnabledCloneTools { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_CustomCloneTools")]
    public string CustomCloneTools { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_SiteTitle")]
    public string SiteTitle { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_SiteLogoUrl")]
    public string SiteLogoUrl { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_SiteCssUrl")]
    public string SiteCssUrl { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_IsCommitAuthorAvatarVisible")]
    public bool IsCommitAuthorAvatarVisible { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_LinksUrl")]
    public string LinksUrl { get; set; }

    [Remote("IsValidRegex", "Validation")]
    [IsValidRegex]
    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_LinksRegex")]
    public string LinksRegex { get; set; }

    public List<SelectListItem> AvailableLanguages { get; set; }
}
