using Gibbon.Git.Server.Repositories;

namespace Gibbon.Git.Server.Data.Entities;

public class RepositoryUserPermission : IRepositoryPermission
{
    public int RepositoryId { get; set; }
    public int UserId { get; set; }
    public RepositoryAccessLevel AccessLevel { get; set; } = RepositoryAccessLevel.Push;

    int IRepositoryPermission.MemberId => UserId;
}
