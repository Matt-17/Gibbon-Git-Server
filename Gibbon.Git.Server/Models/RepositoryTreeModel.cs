namespace Gibbon.Git.Server.Models;

public class RepositoryTreeModel
{
    public string Name { get; set; }
    public string Branch { get; set; }
    public string Path { get; set; }
    public string Readme { get; set; }
    public RepositoryLogoDetailModel Logo { get; set; }
    public IEnumerable<RepositoryTreeDetailModel> Files { get; set; }
    public RepositoryCloneModel Clone { get; set; }
}