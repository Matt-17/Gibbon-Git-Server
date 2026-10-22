using System.Threading.Tasks;

namespace Gibbon.Git.Server.Repositories;

/// <summary>
/// Reads and changes the default branch (HEAD) of repositories with the git CLI.
/// </summary>
public interface IRepositoryHeadService
{
    /// <summary>
    /// Creates an empty bare repository whose HEAD points to the given branch.
    /// </summary>
    Task<bool> InitAsync(string repositoryName, string defaultBranch);

    /// <summary>
    /// Returns the branch HEAD points to (even if it has no commits yet) and all existing branches.
    /// </summary>
    Task<RepositoryBranches> GetBranchesAsync(string repositoryName);

    /// <summary>
    /// Points HEAD to an existing branch.
    /// </summary>
    Task<bool> SetDefaultBranchAsync(string repositoryName, string branch);

    /// <summary>
    /// Points HEAD to an existing branch when it refers to a branch that does not exist.
    /// </summary>
    Task<bool> RepairHeadAsync(string repositoryName);
}

public record RepositoryBranches(string DefaultBranch, IReadOnlyList<string> Branches);
