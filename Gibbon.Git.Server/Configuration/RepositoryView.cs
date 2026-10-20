using System.ComponentModel.DataAnnotations;

namespace Gibbon.Git.Server.Configuration;

public enum RepositoryView
{
    [Display(ResourceType = typeof(Resources), Name = "Repository_Detail_Detail")]
    Detail = 0,

    [Display(ResourceType = typeof(Resources), Name = "Repository_Layout_Browse")]
    Tree = 1,

    [Display(ResourceType = typeof(Resources), Name = "Repository_Layout_Commits")]
    Commits = 2,

    [Display(ResourceType = typeof(Resources), Name = "Repository_Layout_Tags")]
    Tags = 3
}
