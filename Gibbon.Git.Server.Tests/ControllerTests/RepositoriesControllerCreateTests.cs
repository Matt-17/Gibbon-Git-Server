using System;
using System.IO;
using System.Linq;
using System.Security.Claims;

using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Controllers;
using Gibbon.Git.Server.Data;
using Gibbon.Git.Server.Models;
using Gibbon.Git.Server.Repositories;
using Gibbon.Git.Server.Security;
using Gibbon.Git.Server.Services;
using Gibbon.Git.Server.Tests.TestHelper;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace Gibbon.Git.Server.Tests.ControllerTests;

[TestClass]
[TestCategory(TestCategories.UnitTest)]
public class RepositoriesControllerCreateTests : ControllerTestBase<RepositoriesController>
{
    private IRepositoryService _repositoryService = null!;
    private IUserService _userService = null!;
    private ITeamService _teamService = null!;
    private string _repositoriesPath = null!;

    protected override void ConfigureServices(ServiceCollection services)
    {
        services.AddSubstitute<ILogger<RepositoriesController>>();
        _teamService = services.AddSubstitute<ITeamService>();
        _repositoryService = services.AddSubstitute<IRepositoryService>();
        _userService = services.AddSubstitute<IUserService>();
        var permissionService = services.AddSubstitute<IRepositoryPermissionService>();
        services.AddSubstitute<IRepositorySynchronizer>();
        services.AddSingleton(new ServerSettings());
        var pathResolver = services.AddSubstitute<IPathResolver>();
        services.AddSubstitute<IRepositoryBrowserFactory>();
        services.AddSubstitute<IRepositoryLogoService>();

        _repositoriesPath = Path.Combine(Path.GetTempPath(), "gibbon-create-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_repositoriesPath);
        pathResolver.GetRepositories().Returns(_repositoriesPath);
        permissionService.HasCreatePermission(Arg.Any<int>()).Returns(true);

        _repositoryService.NameIsUnique(Arg.Any<string>(), Arg.Any<int>()).Returns(true);
        _repositoryService.Create(Arg.Any<RepositoryModel>()).Returns(true);
        _userService.GetUserModel(Arg.Any<int>()).Returns(x => new UserModel { Id = x.Arg<int>(), Username = "user" + x.Arg<int>() });
        _userService.GetAllUsers().Returns([]);
        _teamService.GetTeam(Arg.Any<int>()).Returns(x => new TeamModel { Id = x.Arg<int>(), Name = "team" + x.Arg<int>() });
        _teamService.GetAllTeams().Returns([]);
    }

    [TestCleanup]
    public void DeleteRepositories()
    {
        if (Directory.Exists(_repositoriesPath))
        {
            foreach (var file in Directory.EnumerateFiles(_repositoriesPath, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            Directory.Delete(_repositoriesPath, true);
        }
    }

    [TestMethod]
    public void Create_StoresSelectedUsersAdministratorsAndTeams()
    {
        var model = new RepositoryDetailModel
        {
            Name = "demo",
            PostedSelectedUsers = [2, 3],
            PostedSelectedAdministrators = [1],
            PostedSelectedTeams = [7]
        };

        var result = Controller.Create(model);

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        _repositoryService.Received(1).Create(Arg.Is<RepositoryModel>(r =>
            r.Name == "demo" &&
            r.Users.Select(u => u.Id).SequenceEqual(new[] { 2, 3 }) &&
            r.Administrators.Select(u => u.Id).SequenceEqual(new[] { 1 }) &&
            r.Teams.Select(t => t.Id).SequenceEqual(new[] { 7 })));
        Assert.IsTrue(Directory.Exists(Path.Combine(_repositoriesPath, "demo")));
    }

    [TestMethod]
    public void Create_ByRegularUser_KeepsCreatorAsAdministrator()
    {
        Controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "5"), new Claim(ClaimTypes.Name, "user5")], "Test"))
        };

        Controller.Create(new RepositoryDetailModel { Name = "mine", PostedSelectedAdministrators = [5] });

        _repositoryService.Received(1).Create(Arg.Is<RepositoryModel>(r => r.Administrators.Single().Id == 5));
    }

    [TestMethod]
    public void Create_InvalidModel_ShowsSelectionAgain()
    {
        _repositoryService.NameIsUnique("taken", Arg.Any<int>()).Returns(false);

        var result = Controller.Create(new RepositoryDetailModel { Name = "taken", PostedSelectedUsers = [2] }) as ViewResult;

        Assert.IsNotNull(result);
        var model = (RepositoryDetailModel)result.Model!;
        Assert.AreEqual(2, model.Users.Single().Id);
        _repositoryService.DidNotReceive().Create(Arg.Any<RepositoryModel>());
    }
}
