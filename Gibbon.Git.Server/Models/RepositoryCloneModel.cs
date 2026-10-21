namespace Gibbon.Git.Server.Models;

public class RepositoryCloneModel
{
    public string Name { get; set; }
    public string GitUrl { get; set; }
    public string PersonalGitUrl { get; set; }
    public string Branch { get; set; }
    public bool CanPush { get; set; }
}
