using System.ComponentModel.DataAnnotations;

namespace Gibbon.Git.Server.Configuration;

public enum NameFormat
{
    [Display(ResourceType = typeof(Resources), Name = "NameFormat_LastCommaFirst")]
    LastCommaFirst = 0,

    [Display(ResourceType = typeof(Resources), Name = "NameFormat_FirstLast")]
    FirstLast = 1,

    [Display(ResourceType = typeof(Resources), Name = "NameFormat_LastFirst")]
    LastFirst = 2
}
