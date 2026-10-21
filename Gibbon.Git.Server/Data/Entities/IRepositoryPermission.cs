using Gibbon.Git.Server.Repositories;

namespace Gibbon.Git.Server.Data.Entities;

public interface IRepositoryPermission
{
    int MemberId { get; }
    RepositoryAccessLevel AccessLevel { get; set; }
}
