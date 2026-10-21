using Gibbon.Git.Server.Repositories;

namespace Gibbon.Git.Server.Data.Entities;

public class RepositoryTeamPermission : IRepositoryPermission
{
    public int RepositoryId { get; set; }
    public int TeamId { get; set; }
    public RepositoryAccessLevel AccessLevel { get; set; } = RepositoryAccessLevel.Push;

    int IRepositoryPermission.MemberId => TeamId;
}
