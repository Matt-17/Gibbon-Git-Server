using System;
using System.Linq;
using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Data;
using Gibbon.Git.Server.Models;
using Gibbon.Git.Server.Repositories;
using Gibbon.Git.Server.Security;
using Gibbon.Git.Server.Tests.TestHelper;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Gibbon.Git.Server.Tests.UserTests;

[TestClass]
public class EfSqlitePermissionServiceTest : DbTestBase<SqliteConnectionFactory>
{
    private IRepositoryPermissionService _service = null!;
    private IRoleProvider _roleProvider = null!;
    private IRepositoryService _repositoryService = null!;
    private ServerSettings _serverSettings = null!;
    private ITeamService _teamsService = null!;

    protected override void ConfigureServices(ServiceCollection services)
    {
        _roleProvider = services.AddSubstitute<IRoleProvider>();
        services.AddSubstitute<IUserService>();
        _teamsService = services.AddSubstitute<ITeamService>();
        _repositoryService = services.AddSubstitute<IRepositoryService>();
        _serverSettings = new ServerSettings();
        services.AddSingleton(_serverSettings);
        services.AddSingleton<IRepositoryPermissionService, RepositoryPermissionService>();
    }

    protected override void UseServices(IServiceProvider serviceProvider)
    {
        _service = serviceProvider.GetRequiredService<IRepositoryPermissionService>();
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that anonymous users cannot create repositories.")]
    public void AnonymousUserCannotCreateRepository()
    {
        var userId = 0;
        var canCreate = _service.HasCreatePermission(userId);
        Assert.IsFalse(canCreate);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that system administrators can create repositories.")]
    public void SystemAdministratorCanCreateRepository()
    {
        var userId = 1;
        _roleProvider.IsUserInRole(userId, Roles.Admin).Returns(true);

        var canCreate = _service.HasCreatePermission(userId);
        Assert.IsTrue(canCreate);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that non-administrator users can create repositories if allowed by settings.")]
    public void NonAdminUserCanCreateRepositoryIfAllowedBySettings()
    {
        var userId = 1;
        _serverSettings.AllowUserRepositoryCreation = true;

        var canCreate = _service.HasCreatePermission(userId);
        Assert.IsTrue(canCreate);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that a user has permission to access a repository with Pull level.")]
    public void UserHasPullPermissionForRepository()
    {
        var userId = 1;
        var repositoryName = "TestRepo";
        var newGuid = 1;
        var repositoryModel = new RepositoryModel
        {
            Id = newGuid,
            Name = repositoryName,
            AnonymousAccess = true
        };
        _repositoryService.GetRepository(repositoryName).Returns(repositoryModel);
        _repositoryService.GetRepository(newGuid).Returns(repositoryModel);

        var hasPermission = _service.HasPermission(userId, repositoryName, RepositoryAccessLevel.Pull);
        Assert.IsTrue(hasPermission);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that an anonymous user has pull permission for a repository allowing anonymous access.")]
    public void AnonymousUserHasPullPermissionForRepositoryWithAnonymousAccess()
    {
        var userId = 0;
        var repositoryName = "PublicRepo";
        var newGuid = 1;
        var repositoryModel = new RepositoryModel { Id = newGuid, Name = repositoryName, AnonymousAccess = true };
        _repositoryService.GetRepository(repositoryName).Returns(repositoryModel);
        _repositoryService.GetRepository(newGuid).Returns(repositoryModel);

        var hasPermission = _service.HasPermission(userId, repositoryName, RepositoryAccessLevel.Pull);
        Assert.IsTrue(hasPermission);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that a user without appropriate roles cannot administer a repository.")]
    public void UserWithoutRoleCannotAdministerRepository()
    {
        var userId = 1;
        var repositoryId = 1;
        _repositoryService.GetRepository(repositoryId).Returns(new RepositoryModel { Id = repositoryId });

        var hasPermission = _service.HasPermission(userId, repositoryId, RepositoryAccessLevel.Administer);
        Assert.IsFalse(hasPermission);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that non-existent repository by name returns false for permission check.")]
    public void NonExistentRepositoryByNameReturnsFalse()
    {
        var adminId = 1;
        _roleProvider.GetRolesForUser(adminId).Returns([Roles.Admin]);
        var hasPermission = _service.HasPermission(adminId, "NonExistentRepos", RepositoryAccessLevel.Pull);
        Assert.IsFalse(hasPermission);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that non-existent repository by Guid throws exception for permission check.")]
    public void NonExistentRepositoryByGuidThrowsException()
    {
        var adminId = 1;
        _roleProvider.GetRolesForUser(adminId).Returns([Roles.Admin]);
        Assert.ThrowsExactly<InvalidOperationException>(() => _service.HasPermission(adminId, 17, RepositoryAccessLevel.Pull));
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that admin is authorised for any repository.")]
    public void AdminIsAuthorisedForAnyRepo()
    {
        var adminId = 1;
        _roleProvider.IsUserInRole(adminId, Roles.Admin).Returns(true);
        var repoId = 1;
        _repositoryService.GetRepository(repoId).Returns(new RepositoryModel { Id = repoId });

        var hasPermission = _service.HasPermission(adminId, repoId, RepositoryAccessLevel.Pull);
        Assert.IsTrue(hasPermission);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that unrelated user is not authorised for repository.")]
    public void UnrelatedUserIsNotAuthorisedForRepo()
    {
        var userId = 1;
        var repoId = 1;
        _repositoryService.GetRepository(repoId).Returns(new RepositoryModel { Id = repoId });
        _teamsService.GetTeamsForUser(userId).Returns([]);

        var hasPermission = _service.HasPermission(userId, repoId, RepositoryAccessLevel.Pull);
        Assert.IsFalse(hasPermission);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that repository member user is authorised for repository.")]
    public void RepoMemberUserIsAuthorised()
    {
        var userId = 1;
        var repoId = 1;
        var repositoryModel = new RepositoryModel { Id = repoId, Users = [new UserModel { Id = userId }] };
        _repositoryService.GetRepository(repoId).Returns(repositoryModel);

        var hasPermission = _service.HasPermission(userId, repoId, RepositoryAccessLevel.Pull);
        Assert.IsTrue(hasPermission);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that repository admin user is authorised for repository.")]
    public void RepoAdminIsAuthorised()
    {
        var userId = 1;
        var repoId = 1;
        var repositoryModel = new RepositoryModel { Id = repoId, Administrators = [new UserModel { Id = userId }] };
        _repositoryService.GetRepository(repoId).Returns(repositoryModel);

        var hasPermission = _service.HasPermission(userId, repoId, RepositoryAccessLevel.Pull);
        Assert.IsTrue(hasPermission);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that non-team member is not authorised for repository.")]
    public void NonTeamMemberIsNotAuthorised()
    {
        var userId = 1;
        var repoId = 1;
        var team = new TeamModel {};
        var repositoryModel = new RepositoryModel { Id = repoId, Teams = [team] };
        _repositoryService.GetRepository(repoId).Returns(repositoryModel);
        _teamsService.GetTeamsForUser(userId).Returns([]);

        var hasPermission = _service.HasPermission(userId, repoId, RepositoryAccessLevel.Pull);
        Assert.IsFalse(hasPermission);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that team member is authorised for repository.")]
    public void TeamMemberIsAuthorised()
    {
        const int userId = 1;
        const int repoId = 1;

        var team = new TeamModel { Members = [new UserModel { Id = userId }] };
        var repositoryModel = new RepositoryModel { Id = repoId, Teams = [team] };
        _teamsService.GetTeamsForUser(userId).Returns([team]);
        _repositoryService.GetRepository(repoId).Returns(repositoryModel);

        var hasPermission = _service.HasPermission(userId, repoId, RepositoryAccessLevel.Pull);
        Assert.IsTrue(hasPermission);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that system admin is always repository admin.")]
    public void SystemAdminIsAlwaysRepositoryAdmin()
    {
        const int userId = 1;
        const int repoId = 1;
        _repositoryService.GetRepository(repoId).Returns(new RepositoryModel { Id = repoId });
        _roleProvider.IsUserInRole(userId, Roles.Admin).Returns(true);
        var hasPermission = _service.HasPermission(userId, repoId, RepositoryAccessLevel.Administer);
        Assert.IsTrue(hasPermission);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that a normal user is not repository admin.")]
    public void NormalUserIsNotRepositoryAdmin()
    {
        var userId = 1;
        var repoId = 1;
        var repositoryModel = new RepositoryModel { Id = repoId, Users = [new UserModel { Id = userId }] };
        _repositoryService.GetRepository(repoId).Returns(repositoryModel);

        var hasPermission = _service.HasPermission(userId, repoId, RepositoryAccessLevel.Administer);
        Assert.IsFalse(hasPermission);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that a default repository does not allow anonymous access.")]
    public void DefaultRepositoryDoesNotAllowAnonAccess()
    {
        var repoId = 1;
        _repositoryService.GetRepository(repoId).Returns(new RepositoryModel { Id = repoId });

        Assert.IsFalse(_service.HasPermission(0, repoId, RepositoryAccessLevel.Pull));
        Assert.IsFalse(_service.HasPermission(0, "TestRepo", RepositoryAccessLevel.Pull));
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that allowing anonymous push globally does not affect a repository without anonymous access.")]
    public void AllowAnonymousPushDoesNotAffectDefaultRepository()
    {
        var repoId = 1;
        _repositoryService.GetRepository(repoId).Returns(new RepositoryModel { Id = repoId });

        _serverSettings.AllowAnonymousPush = true;

        Assert.IsFalse(_service.HasPermission(0, repoId, RepositoryAccessLevel.Push));
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that anonymous access can be permitted by repository property.")]
    public void AnonAccessCanBePermittedWithRepoProperty()
    {
        var repoId = 1;
        var repository = new RepositoryModel { Id = repoId, AnonymousAccess = true };
        _repositoryService.GetRepository(repoId).Returns(repository);

        Assert.IsTrue(_service.HasPermission(0, repoId, RepositoryAccessLevel.Pull));
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that anonymous access does not allow push by default.")]
    public void AnonAccessDoesNotAllowPushByDefault()
    {
        var repoId = 1;
        var repository = new RepositoryModel { Id = repoId, AnonymousAccess = true };
        _repositoryService.GetRepository(repoId).Returns(repository);

        Assert.IsFalse(_service.HasPermission(0, repoId, RepositoryAccessLevel.Push));
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that anonymous push can be enabled via server configuration.")]
    public void AnonPushCanBeEnabledWithConfig()
    {
        var repoId = 1;
        var repository = new RepositoryModel { Id = repoId, AnonymousAccess = true };
        _repositoryService.GetRepository(repoId).Returns(repository);

        _serverSettings.AllowAnonymousPush = true;

        Assert.IsTrue(_service.HasPermission(0, repoId, RepositoryAccessLevel.Push));
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that GetAllPermitted returns only repositories permitted for a specific user.")]
    public void GetAllPermittedReturnsOnlyRepositoriesPermittedForUser()
    {
        var userId = 1;
        var repo1 = new RepositoryModel {  Name = "Repo1", Users = [new UserModel { Id = userId }] };
        var repo2 = new RepositoryModel {  Name = "Repo2" };
        _repositoryService.GetAllRepositories().Returns([repo1, repo2]);
        _teamsService.GetTeamsForUser(userId).Returns([]);

        var permittedRepos = _service.GetAllPermittedRepositories(userId, RepositoryAccessLevel.Pull);
        CollectionAssert.AreEqual(new[] { "Repo1" }, permittedRepos.Select(r => r.Name).ToArray());
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that GetAllPermitted returns all repositories for system administrators.")]
    public void GetAllPermittedReturnsAllRepositoriesToSystemAdmin()
    {
        var adminId = 1;
        _roleProvider.IsUserInRole(adminId, Roles.Admin).Returns(true);

        var repo1 = new RepositoryModel {  Name = "Repo1" };
        var repo2 = new RepositoryModel {  Name = "Repo2" };
        _repositoryService.GetAllRepositories().Returns([repo1, repo2]);

        var permittedRepos = _service.GetAllPermittedRepositories(adminId, RepositoryAccessLevel.Pull);
        CollectionAssert.AreEqual(new[] { "Repo1", "Repo2" }, permittedRepos.Select(r => r.Name).ToArray());
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that an anonymous repository is permitted to anybody to pull.")]
    public void AnonymousRepoIsPermittedToAnybodyToPull()
    {
        var repo = new RepositoryModel {  Name = "Repo1", AnonymousAccess = true };
        _repositoryService.GetRepository(repo.Id).Returns(repo);

        var hasPermission = _service.HasPermission(0, repo.Id, RepositoryAccessLevel.Pull);
        Assert.IsTrue(hasPermission);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that a new repository is not permitted to an anonymous user.")]
    public void NewRepositoryNotPermittedToAnonymousUser()
    {
        var repo = new RepositoryModel {  Name = "Repo1" };
        _repositoryService.GetRepository(repo.Id).Returns(repo);

        var hasPermission = _service.HasPermission(0, repo.Id, RepositoryAccessLevel.Pull);
        Assert.IsFalse(hasPermission);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that a read-only member can pull but not push.")]
    public void ReadOnlyMemberCanPullButNotPush()
    {
        const int userId = 1;
        const int repoId = 1;
        var repositoryModel = new RepositoryModel { Id = repoId, Users = [new UserModel { Id = userId }], UserAccessLevels = new() { [userId] = RepositoryAccessLevel.Pull } };
        _repositoryService.GetRepository(repoId).Returns(repositoryModel);
        _teamsService.GetTeamsForUser(userId).Returns([]);

        Assert.IsTrue(_service.HasPermission(userId, repoId, RepositoryAccessLevel.Pull));
        Assert.IsFalse(_service.HasPermission(userId, repoId, RepositoryAccessLevel.Push));
        Assert.IsFalse(_service.HasPermission(userId, repoId, RepositoryAccessLevel.Administer));
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that a member without an explicit level can push.")]
    public void MemberWithoutLevelCanPush()
    {
        const int userId = 1;
        const int repoId = 1;
        var repositoryModel = new RepositoryModel { Id = repoId, Users = [new UserModel { Id = userId }] };
        _repositoryService.GetRepository(repoId).Returns(repositoryModel);
        _teamsService.GetTeamsForUser(userId).Returns([]);

        Assert.IsTrue(_service.HasPermission(userId, repoId, RepositoryAccessLevel.Push));
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that members of a read-only team can pull but not push.")]
    public void ReadOnlyTeamMemberCanPullButNotPush()
    {
        const int userId = 1;
        const int repoId = 1;
        var team = new TeamModel { Id = 5, Members = [new UserModel { Id = userId }] };
        var repositoryModel = new RepositoryModel { Id = repoId, Teams = [team], TeamAccessLevels = new() { [team.Id] = RepositoryAccessLevel.Pull } };
        _repositoryService.GetRepository(repoId).Returns(repositoryModel);
        _teamsService.GetTeamsForUser(userId).Returns([team]);

        Assert.IsTrue(_service.HasPermission(userId, repoId, RepositoryAccessLevel.Pull));
        Assert.IsFalse(_service.HasPermission(userId, repoId, RepositoryAccessLevel.Push));
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that the highest level of direct and team membership applies.")]
    public void HighestMemberLevelApplies()
    {
        const int userId = 1;
        const int repoId = 1;
        var readTeam = new TeamModel { Id = 5, Members = [new UserModel { Id = userId }] };
        var writeTeam = new TeamModel { Id = 6, Members = [new UserModel { Id = userId }] };
        var repositoryModel = new RepositoryModel
        {
            Id = repoId,
            Users = [new UserModel { Id = userId }],
            Teams = [readTeam, writeTeam],
            UserAccessLevels = new() { [userId] = RepositoryAccessLevel.Pull },
            TeamAccessLevels = new() { [readTeam.Id] = RepositoryAccessLevel.Pull, [writeTeam.Id] = RepositoryAccessLevel.Push }
        };
        _repositoryService.GetRepository(repoId).Returns(repositoryModel);
        _teamsService.GetTeamsForUser(userId).Returns([readTeam, writeTeam]);

        Assert.IsTrue(_service.HasPermission(userId, repoId, RepositoryAccessLevel.Push));
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryPermission)]
    [Description("Verify that a read-only member keeps anonymous push when the repository allows it.")]
    public void ReadOnlyMemberCanPushWhenAnonymousPushIsAllowed()
    {
        const int userId = 1;
        const int repoId = 1;
        var repositoryModel = new RepositoryModel
        {
            Id = repoId,
            AnonymousAccess = true,
            AllowAnonymousPush = RepositoryPushMode.Yes,
            Users = [new UserModel { Id = userId }],
            UserAccessLevels = new() { [userId] = RepositoryAccessLevel.Pull }
        };
        _repositoryService.GetRepository(repoId).Returns(repositoryModel);
        _teamsService.GetTeamsForUser(userId).Returns([]);

        Assert.IsTrue(_service.HasPermission(userId, repoId, RepositoryAccessLevel.Push));
    }
}
