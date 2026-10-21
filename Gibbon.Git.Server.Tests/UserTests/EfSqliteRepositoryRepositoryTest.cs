using System;
using System.Linq;
using System.Text;

using Gibbon.Git.Server.Data;
using Gibbon.Git.Server.Data.Entities;
using Gibbon.Git.Server.Models;
using Gibbon.Git.Server.Repositories;
using Gibbon.Git.Server.Security;
using Gibbon.Git.Server.Tests.TestHelper;

using Microsoft.Extensions.DependencyInjection;

namespace Gibbon.Git.Server.Tests.UserTests;

[TestClass]
public class EfSqliteRepositoryRepositoryTest : DbTestBase<SqliteConnectionFactory>
{
    private IRepositoryService _repositoryService = null!;
    private IUserService _memberService = null!;
    private ITeamService _teamService = null!;

    protected override void ConfigureServices(ServiceCollection services)
    {
        services.AddSingleton<IRepositoryService, RepositoryService>();

        _memberService = services.AddSubstitute<IUserService>();

        _teamService = services.AddSubstitute<ITeamService>();
    }

    protected override void UseServices(IServiceProvider serviceProvider)
    {
        _repositoryService = serviceProvider.GetRequiredService<IRepositoryService>();
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that a new repository is empty.")]
    public void NewRepoIsEmpty()
    {
        Assert.AreEqual(0, _repositoryService.GetAllRepositories().Count);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that a repository with no users can be added.")]
    public void RespositoryWithNoUsersCanBeAdded()
    {
        var newRepo = MakeRepo("Repo1");

        _repositoryService.Create(newRepo);

        Assert.AreEqual("Repo1", _repositoryService.GetAllRepositories().Single().Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that adding a duplicate repository name returns false.")]
    public void DuplicateRepoNameAddReturnsFalse()
    {
        Assert.IsTrue(_repositoryService.Create(MakeRepo("Repo1")));
        Assert.IsFalse(_repositoryService.Create(MakeRepo("Repo1")));
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that a repository name cannot be created if it differs only in case.")]
    public void RepoNameDifferentCaseCannotBeCreated()
    {
        Assert.IsTrue(_repositoryService.Create(MakeRepo("name")));
        Assert.IsFalse(_repositoryService.Create(MakeRepo("NAME")));
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that a new repository name is unique.")]
    public void NewRepoNameIsUnique()
    {
        _repositoryService.Create(MakeRepo("abc"));
        Assert.IsTrue(_repositoryService.NameIsUnique("x", 0));
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that a duplicate repository name is not unique even if the case differs.")]
    public void DuplicateRepoNameIsNotUniqueEvenIfCaseDiffers()
    {
        _repositoryService.Create(MakeRepo("abc"));
        Assert.IsFalse(_repositoryService.NameIsUnique("ABC", 0));
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that a duplicate repository name is allowed if it is the current repository.")]
    public void DuplicateRepoNameIsAllowedIfCurrentRepo()
    {
        var repo = MakeRepo("abc");
        _repositoryService.Create(repo);
        Assert.IsTrue(_repositoryService.NameIsUnique("ABC", repo.Id));
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that repository retrieval is case insensitive.")]
    public void GetRepoIsCaseInsensitive()
    {
        var model = MakeRepo("aaa");
        Assert.IsTrue(_repositoryService.Create(model));
        Assert.AreEqual(model.Id, _repositoryService.GetRepository("aaa").Id);
        Assert.AreEqual(model.Id, _repositoryService.GetRepository("aAa").Id);
        Assert.AreEqual(model.Id, _repositoryService.GetRepository("AAA").Id);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that a repository with users can be added.")]
    public void RespositoryWithUsersCanBeAdded()
    {
        var newRepo = MakeRepo("Repo1");
        newRepo.Users =
        [
            AddUserFred()
        ];

        _repositoryService.Create(newRepo);

        Assert.AreEqual("Blogs, Fred", _repositoryService.GetAllRepositories().Single().Users.Single().DisplayName);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that a repository with an administrator can be added.")]
    public void RespositoryWithAdministratorCanBeAdded()
    {
        var newRepo = MakeRepo("Repo1");
        newRepo.Administrators =
        [
            AddUserFred()
        ];

        _repositoryService.Create(newRepo);

        Assert.AreEqual("Blogs, Fred", _repositoryService.GetAllRepositories().Single().Administrators.Single().DisplayName);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that a new repository can be retrieved by its ID.")]
    public void NewRepoCanBeRetrievedById()
    {
        var newRepo1 = MakeRepo("Repo1");
        _repositoryService.Create(newRepo1);

        Assert.AreEqual("Repo1", _repositoryService.GetRepository(newRepo1.Id).Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that attempting to retrieve a non-existent repository by ID throws an exception.")]
    public void NonExistentRepoIdThrowsException()
    {
        var newRepo1 = MakeRepo("Repo1");
        _repositoryService.Create(newRepo1);
        Assert.ThrowsExactly<InvalidOperationException>(() => _repositoryService.GetRepository(17));
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that attempting to retrieve a non-existent repository by name returns null.")]
    public void NonExistentRepoNameReturnsNull()
    {
        var newRepo1 = MakeRepo("Repo1");
        _repositoryService.Create(newRepo1);

        Assert.IsNull(_repositoryService.GetRepository("Repo2"));
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that a new repository can be retrieved by its name.")]
    public void NewRepoCanBeRetrievedByName()
    {
        var newRepo1 = MakeRepo("Repo1");
        _repositoryService.Create(newRepo1);

        Assert.AreEqual("Repo1", _repositoryService.GetRepository("Repo1").Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that a new repository can be deleted.")]
    public void NewRepoCanBeDeleted()
    {
        _repositoryService.Create(MakeRepo("Repo1"));
        _repositoryService.Create(MakeRepo("Repo2"));

        _repositoryService.Delete(_repositoryService.GetRepository("Repo1").Id);

        Assert.AreEqual("Repo2", _repositoryService.GetAllRepositories().Single().Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that deleting a non-existent repository is silently ignored.")]
    public void DeletingMissingRepoIsSilentlyIgnored()
    {
        _repositoryService.Create(MakeRepo("Repo1"));

        _repositoryService.Delete(17);

        Assert.AreEqual("Repo1", _repositoryService.GetAllRepositories().Single().Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that repository properties are saved on update.")]
    public void RepoSimplePropertiesAreSavedOnUpdate()
    {
        var repo = MakeRepo("Repo1");
        _repositoryService.Create(repo);

        repo.Name = "SonOfRepo";
        repo.Group = "RepoGroup";
        repo.AnonymousAccess = true;
        repo.Description = "New desc";

        _repositoryService.Update(repo);

        var readBackRepo = _repositoryService.GetRepository("Repo1");
        Assert.AreEqual(repo.Group, readBackRepo.Group);
        Assert.AreEqual(repo.AnonymousAccess, readBackRepo.AnonymousAccess);
        Assert.AreEqual(repo.Description, readBackRepo.Description);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that repository can not be updated to change its name.")]
    public void RepoCanNotBeUpdatedToChangeName()
    {
        const string repoName = "Repo1";
        const string newRepoName = "SonOfRepo";

        var repo = MakeRepo(repoName);
        _repositoryService.Create(repo);

        repo.Name = newRepoName;

        _repositoryService.Update(repo);

        var dbRepo = _repositoryService.GetRepository(newRepoName);
        Assert.IsNull(dbRepo);
        dbRepo = _repositoryService.GetRepository(repoName);
        Assert.IsNotNull(dbRepo);
        Assert.AreEqual(repoName, dbRepo.Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that a logo can be added to a repository during its creation.")]
    public void RepoLogoCanBeAddedAtCreation()
    {
        var repo = MakeRepo("Repo1");
        var logoBytes = Encoding.UTF8.GetBytes("Hello");
        repo.Logo = logoBytes;
        _repositoryService.Create(repo);

        var readBackRepo = _repositoryService.GetRepository("Repo1");
        CollectionAssert.AreEqual(logoBytes, readBackRepo.Logo);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that a logo can be added to an existing repository via update.")]
    public void RepoLogoCanBeAddedWithUpdate()
    {
        var repo = MakeRepo("Repo1");
        _repositoryService.Create(repo);

        var logoBytes = Encoding.UTF8.GetBytes("Hello");
        repo.Logo = logoBytes;

        _repositoryService.Update(repo);

        var readBackRepo = _repositoryService.GetRepository("Repo1");
        CollectionAssert.AreEqual(logoBytes, readBackRepo.Logo);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that a logo can be removed from a repository via update.")]
    public void RepoLogoCanBeRemovedWithUpdate()
    {
        var repo = MakeRepo("Repo1");
        _repositoryService.Create(repo);

        repo.Logo = Encoding.UTF8.GetBytes("Hello");
        _repositoryService.Update(repo);
        repo.RemoveLogo = true;
        _repositoryService.Update(repo);

        Assert.IsNull(_repositoryService.GetRepository("Repo1").Logo);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that a repository logo is preserved when null is provided in an update.")]
    public void RepoLogoIsPreservedWhenNullAtUpdate()
    {
        var logoBytes = Encoding.UTF8.GetBytes("Hello");
        var repo = MakeRepo("Repo1");
        repo.Logo = logoBytes;
        _repositoryService.Create(repo);

        var updateRepo = new RepositoryModel
        {
            Id = repo.Id,
            Name = repo.Name,
            Logo = null,
            Users = [],
            Administrators = [],
            Teams = []
        };
        _repositoryService.Update(updateRepo);

        CollectionAssert.AreEqual(logoBytes, _repositoryService.GetRepository("Repo1").Logo);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that a newly created repository is not permitted to any users, teams, or administrators by default.")]
    public void NewRepositoryIsPermittedToNobody()
    {
        _repositoryService.Create(MakeRepo("Repo1"));

        Assert.AreEqual(0, _repositoryService.GetRepository("Repo1").Administrators.Length);
        Assert.AreEqual(0, _repositoryService.GetRepository("Repo1").Teams.Length);
        Assert.AreEqual(0, _repositoryService.GetRepository("Repo1").Users.Length);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that a repository assigned to a team is reported as accessible to that team.")]
    public void RepositoryIsReportedAsAccessibleToTeam()
    {
        var team = AddTeam();
        var repoWithTeam = MakeRepo("Repo1");
        repoWithTeam.Teams = [team];
        _repositoryService.Create(repoWithTeam);
        var repoWithoutTeam = MakeRepo("Repo2");
        _repositoryService.Create(repoWithoutTeam);

        CollectionAssert.AreEqual(new[] { "Repo1" }, _repositoryService.GetTeamRepositories(team.Id).Select(x => x.Name).ToList());
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that no repositories are listed if a team is not assigned to any repositories.")]
    public void NoReposistoriesListedIfNoneInTeam()
    {
        var team = AddTeam();
        var repoWithoutTeam1 = MakeRepo("Repo1");
        _repositoryService.Create(repoWithoutTeam1);
        var repoWithoutTeam2 = MakeRepo("Repo2");
        _repositoryService.Create(repoWithoutTeam2);

        Assert.AreEqual(0, _repositoryService.GetTeamRepositories(team.Id).Count);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that members get write access unless a level is given.")]
    public void MembersDefaultToWriteAccess()
    {
        var newRepo = MakeRepo("Repo1");
        var user = AddUserFred();
        var team = AddTeam();
        newRepo.Users = [user];
        newRepo.Teams = [team];

        _repositoryService.Create(newRepo);

        var repo = _repositoryService.GetRepository("Repo1");
        Assert.AreEqual(RepositoryAccessLevel.Push, repo.UserAccessLevels[user.Id]);
        Assert.AreEqual(RepositoryAccessLevel.Push, repo.TeamAccessLevels[team.Id]);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that read-only member levels are saved on create.")]
    public void MemberAccessLevelsAreSavedOnCreate()
    {
        var newRepo = MakeRepo("Repo1");
        var user = AddUserFred();
        var team = AddTeam();
        newRepo.Users = [user];
        newRepo.Teams = [team];
        newRepo.UserAccessLevels = new() { [user.Id] = RepositoryAccessLevel.Pull };
        newRepo.TeamAccessLevels = new() { [team.Id] = RepositoryAccessLevel.Pull };

        _repositoryService.Create(newRepo);

        var repo = _repositoryService.GetAllRepositories().Single();
        Assert.AreEqual(RepositoryAccessLevel.Pull, repo.UserAccessLevels[user.Id]);
        Assert.AreEqual(RepositoryAccessLevel.Pull, repo.TeamAccessLevels[team.Id]);
    }

    [TestMethod]
    [TestCategory(TestCategories.RepositoryService)]
    [Description("Verify that member levels can be changed and members removed on update.")]
    public void MemberAccessLevelsCanBeChangedOnUpdate()
    {
        var newRepo = MakeRepo("Repo1");
        var user = AddUserFred();
        var team = AddTeam();
        newRepo.Users = [user];
        newRepo.Teams = [team];
        _repositoryService.Create(newRepo);

        var repo = _repositoryService.GetRepository("Repo1");
        repo.UserAccessLevels = new() { [user.Id] = RepositoryAccessLevel.Pull };
        _repositoryService.Update(repo);

        repo = _repositoryService.GetRepository("Repo1");
        Assert.AreEqual(RepositoryAccessLevel.Pull, repo.UserAccessLevels[user.Id]);
        Assert.AreEqual(RepositoryAccessLevel.Push, repo.TeamAccessLevels[team.Id]);

        repo.Teams = [];
        _repositoryService.Update(repo);

        repo = _repositoryService.GetRepository("Repo1");
        Assert.AreEqual(RepositoryAccessLevel.Pull, repo.UserAccessLevels[user.Id]);
        Assert.AreEqual(0, repo.Teams.Length);
        Assert.AreEqual(0, repo.TeamAccessLevels.Count);
    }

    private static RepositoryModel MakeRepo(string name)
    {
        return new RepositoryModel
        {
            Name = name
        };
    }

    private UserModel AddUserFred()
    {
        User user = new User
        {

            Username = "fred",
            Password = "letmein",
            PasswordSalt = "salt",
            GivenName = "Fred",
            Surname = "Blogs",
            Email = "fred@aol"
        };

        DbContext.Users.Add(user);
        DbContext.SaveChanges();

        return new UserModel
        {
            Id = user.Id,
            Username = user.Username,
            GivenName = user.GivenName,
            Surname = user.Surname,
            Email = user.Email
        };
    }

    private TeamModel AddTeam()
    {
        Team team = new Team
        {

            Name = "Team1",
            Description = "Team1 description",
            Repositories = []
        };
        DbContext.Teams.Add(team);
        DbContext.SaveChanges();

        TeamModel newTeam = new TeamModel
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            Members = []
        };

        return newTeam;
    }
}
