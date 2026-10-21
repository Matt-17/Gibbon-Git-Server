using Gibbon.Git.Server.Data;
using Gibbon.Git.Server.Helpers;
using Gibbon.Git.Server.Repositories;

namespace Gibbon.Git.Server.Models;

public class RepositoryModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Group { get; set; }
    public string Description { get; set; }
    public bool AnonymousAccess { get; set; }
    public RepositoryPushMode AllowAnonymousPush { get; set; } = RepositoryPushMode.Global;
    public UserModel[] Users { get; set; } = [];
    public UserModel[] Administrators { get; set; } = [];
    public TeamModel[] Teams { get; set; } = [];
    public Dictionary<int, RepositoryAccessLevel> UserAccessLevels { get; set; } = [];
    public Dictionary<int, RepositoryAccessLevel> TeamAccessLevels { get; set; } = [];
    public byte[] Logo { get; set; }
    public bool RemoveLogo { get; set; }
    public string LinksRegex { get; set; } = "";
    public string LinksUrl { get; set; } = "";
    public bool LinksUseGlobal { get; set; } = true;

    public bool NameIsValid => StringHelper.NameIsValid(Name);

    public RepositoryAccessLevel GetUserAccessLevel(int userId) => UserAccessLevels.GetValueOrDefault(userId, RepositoryAccessLevel.Push);

    public RepositoryAccessLevel GetTeamAccessLevel(int teamId) => TeamAccessLevels.GetValueOrDefault(teamId, RepositoryAccessLevel.Push);
}
