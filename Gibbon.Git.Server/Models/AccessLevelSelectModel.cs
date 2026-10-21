using Gibbon.Git.Server.Repositories;

namespace Gibbon.Git.Server.Models;

public record AccessLevelSelectModel(string Name, RepositoryAccessLevel Level);
