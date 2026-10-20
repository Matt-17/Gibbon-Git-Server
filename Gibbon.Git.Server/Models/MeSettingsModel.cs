using System.ComponentModel.DataAnnotations;
using Gibbon.Git.Server.Configuration;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Gibbon.Git.Server.Models;

public class MeSettingsModel
{
    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_DefaultLanguage")]
    public string PreferredLanguage { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_DisplayNameFormat")]
    public NameFormat? DisplayNameFormat { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_SortNameFormat")]
    public NameFormat? SortNameFormat { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_DateFormat")]
    public DateFormat? DateFormat { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_TimeFormat")]
    public TimeFormat? TimeFormat { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_DefaultRepositoryView")]
    public RepositoryView? DefaultRepositoryView { get; set; }

    /// <summary>
    /// This is the list of available languages for the user to choose from.
    /// </summary>
    /// <remarks>
    /// This is just for the user to choose from, why we don't need a display attribute.
    /// </remarks>
    internal List<SelectListItem> AvailableLanguages { get; set; }
}
