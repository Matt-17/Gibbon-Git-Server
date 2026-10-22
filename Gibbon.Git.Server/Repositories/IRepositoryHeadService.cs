using System.Threading.Tasks;

namespace Gibbon.Git.Server.Repositories;

public interface IRepositoryHeadService
{
    /// <summary>
    /// Points HEAD to an existing branch when it refers to a branch that does not exist.
    /// </summary>
    Task<bool> RepairHeadAsync(string repositoryName);
}
