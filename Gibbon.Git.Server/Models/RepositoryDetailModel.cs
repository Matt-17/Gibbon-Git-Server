using System.ComponentModel.DataAnnotations;
using Gibbon.Git.Server.Data;
using Gibbon.Git.Server.Middleware.Attributes;
using Gibbon.Git.Server.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Gibbon.Git.Server.Models;

public class RepositoryDetailModel
{
    public int Id { get; set; }

    [Remote("UniqueNameRepo", "Validation", AdditionalFields = "Id", ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Validation_Duplicate_Name")]
    [Required(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Validation_Required")]
    [RepositoryName(ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Validation_FileName")]
    [StringLength(50, ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Validation_StringLength")]
    [Display(ResourceType = typeof(Resources), Name = "Repository_Detail_Name")]
    public string Name { get; set; } = "";

    [Display(ResourceType = typeof(Resources), Name = "Repository_Detail_Group")]
    [StringLength(255, ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Validation_StringLength")]
    public string Group { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Repository_Detail_Description")]
    [StringLength(255, ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Validation_StringLength")]
    public string Description { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Repository_Detail_Users")]
    public UserModel[] Users { get; set; } = [];
    public int[] PostedSelectedUsers { get; set; } = [];
    public UserModel[] AllUsers { get; set; } = [];
    public Dictionary<int, RepositoryAccessLevel> UserAccessLevels { get; set; } = [];

    [Display(ResourceType = typeof(Resources), Name = "Repository_Detail_Teams")]
    public TeamModel[] Teams { get; set; } = [];
    public int[] PostedSelectedTeams { get; set; } = [];
    public TeamModel[] AllTeams { get; set; } = [];
    public Dictionary<int, RepositoryAccessLevel> TeamAccessLevels { get; set; } = [];

    [Display(ResourceType = typeof(Resources), Name = "Repository_Detail_Administrators")]
    public UserModel[] Administrators { get; set; } = [];
    public int[] PostedSelectedAdministrators { get; set; } = [];
    public UserModel[] AllAdministrators { get; set; } = [];

    [Display(ResourceType = typeof(Resources), Name = "Repository_Detail_IsCurrentUserAdmin")]
    public bool IsCurrentUserAdministrator { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Repository_Detail_Anonymous")]
    public bool AllowAnonymous { get; set; }

    [EnumDataType(typeof(RepositoryPushMode), ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "Repository_Edit_InvalidAnonymousPushMode")]
    [Display(ResourceType = typeof(Resources), Name = "Repository_Detail_AllowAnonymousPush")]
    public RepositoryPushMode AllowAnonymousPush { get; set; } = RepositoryPushMode.Global;

    [Display(ResourceType = typeof(Resources), Name = "Repository_Detail_Status")]
    public RepositoryDetailStatus Status { get; set; }

    public RepositoryLogoDetailModel Logo { get; set; }
    public string GitUrl { get; set; }
    public string PersonalGitUrl { get; set; }
    public RepositoryCloneModel Clone { get; set; }

    [Remote("IsValidRegex", "Validation")]
    [IsValidRegex]
    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_LinksRegex")]
    public string LinksRegex { get; set; }
    [Display(ResourceType = typeof(Resources), Name = "Settings_Global_LinksUrl")]
    public string LinksUrl { get; set; }
    [Display(ResourceType = typeof(Resources), Name = "Repository_Detail_LinksUseGlobal")]
    public bool LinksUseGlobal { get; set; } = true;
}
