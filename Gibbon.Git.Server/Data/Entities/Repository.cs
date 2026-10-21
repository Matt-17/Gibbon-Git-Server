namespace Gibbon.Git.Server.Data.Entities;

public class Repository
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Group { get; set; }
    public string Description { get; set; }
    public bool Anonymous { get; set; }
    public byte[] Logo { get; set; }
    public RepositoryPushMode AllowAnonymousPush { get; set; }
    public string LinksRegex { get; set; }
    public string LinksUrl { get; set; }
    public bool LinksUseGlobal { get; set; } = true;

    public virtual ICollection<Team> Teams { get; set; } = new HashSet<Team>();
    public virtual ICollection<User> Users { get; set; } = new HashSet<User>();
    public virtual ICollection<RepositoryUserPermission> UserPermissions { get; set; } = new HashSet<RepositoryUserPermission>();
    public virtual ICollection<RepositoryTeamPermission> TeamPermissions { get; set; } = new HashSet<RepositoryTeamPermission>();
}
