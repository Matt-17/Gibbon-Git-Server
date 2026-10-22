using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Gibbon.Git.Server.Tests.ControllerTests;

[TestClass]
[TestCategory(TestCategories.UnitTest)]
public class RepositoriesControllerCreateTests : ControllerTestBase<RepositoriesController>
{
    private IRepositoryService _repositoryService = null!;
    private IUserService _userService = null!;
    private ITeamService _teamService = null!;
    private IRepositoryCopier _repositoryCopier = null!;
    private string _repositoriesPath = null!;

    protected override void ConfigureServices(ServiceCollection services)
    {
        services.AddSubstitute<ILogger<RepositoriesController>>();
        _teamService = services.AddSubstitute<ITeamService>();
        _repositoryService = services.AddSubstitute<IRepositoryService>();
        _userService = services.AddSubstitute<IUserService>();
        var permissionService = services.AddSubstitute<IRepositoryPermissionService>();
        services.AddSubstitute<IRepositorySynchronizer>();
        services.AddSingleton(new ServerSettings { DefaultBranch = "trunk" });
        var pathResolver = services.AddSubstitute<IPathResolver>();
        services.AddSingleton<IRepositoryHeadService>(new RepositoryHeadService(NullLogger<RepositoryHeadService>.Instance, Options.Create(new GitSettings()), new ProcessService(), pathResolver));
        services.AddSubstitute<IRepositoryBrowserFactory>();
        services.AddSubstitute<IRepositoryLogoService>();
        _repositoryCopier = services.AddSubstitute<IRepositoryCopier>();
        services.AddSubstitute<IUserSettingsService>();

        _repositoriesPath = Path.Combine(Path.GetTempPath(), "gibbon-create-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_repositoriesPath);
        pathResolver.GetRepositories().Returns(_repositoriesPath);
        pathResolver.GetRepositoryPath(Arg.Any<string>()).Returns(x => Path.Combine(_repositoriesPath, x.Arg<string>()));
        pathResolver.ResolveExecutable(Arg.Any<string>()).Returns(x => x.Arg<string>());
        pathResolver.ResolveRoot(Arg.Any<string>()).Returns(_repositoriesPath);
        permissionService.HasCreatePermission(Arg.Any<int>()).Returns(true);

        _repositoryService.NameIsUnique(Arg.Any<string>(), Arg.Any<int>()).Returns(true);
        _repositoryService.Create(Arg.Any<RepositoryModel>()).Returns(true);
        _userService.GetUserModel(Arg.Any<int>()).Returns(x => new UserModel { Id = x.Arg<int>(), Username = "user" + x.Arg<int>() });
        _userService.GetAllUsers().Returns([]);
        _teamService.GetTeam(Arg.Any<int>()).Returns(x => new TeamModel { Id = x.Arg<int>(), Name = "team" + x.Arg<int>() });
        _teamService.GetAllTeams().Returns([]);
        _repositoryCopier.CopyAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
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
    public async Task Create_StoresSelectedUsersAccessLevelsAndTeams()
    {
        var model = new RepositoryDetailModel
        {
            Name = "demo",
            PostedSelectedUsers = [1, 2, 3],
            UserAccessLevels = new() { [1] = RepositoryAccessLevel.Administer, [3] = RepositoryAccessLevel.Pull },
            PostedSelectedTeams = [7]
        };

        var result = await Controller.Create(model);

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        _repositoryService.Received(1).Create(Arg.Is<RepositoryModel>(r =>
            r.Name == "demo" &&
            r.Users.Select(u => u.Id).SequenceEqual(new[] { 1, 2, 3 }) &&
            r.GetUserAccessLevel(1) == RepositoryAccessLevel.Administer &&
            r.GetUserAccessLevel(2) == RepositoryAccessLevel.Push &&
            r.GetUserAccessLevel(3) == RepositoryAccessLevel.Pull &&
            r.Teams.Select(t => t.Id).SequenceEqual(new[] { 7 })));
        Assert.IsTrue(Directory.Exists(Path.Combine(_repositoriesPath, "demo")));
    }

    [TestMethod]
    public async Task Create_StartsRepositoryWithServerDefaultBranch()
    {
        await Controller.Create(new RepositoryDetailModel { Name = "demo", PostedSelectedUsers = [1], UserAccessLevels = new() { [1] = RepositoryAccessLevel.Administer } });

        using var git = Process.Start(new ProcessStartInfo("git", ["-C", Path.Combine(_repositoriesPath, "demo"), "symbolic-ref", "HEAD"]) { RedirectStandardOutput = true, UseShellExecute = false })!;
        var head = git.StandardOutput.ReadToEnd().Trim();
        git.WaitForExit();
        Assert.AreEqual("refs/heads/trunk", head);
    }

    [TestMethod]
    public async Task Create_ByRegularUser_KeepsCreatorAsAdministrator()
    {
        Controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "5"), new Claim(ClaimTypes.Name, "user5")], "Test"))
        };

        await Controller.Create(new RepositoryDetailModel { Name = "mine", PostedSelectedUsers = [5], UserAccessLevels = new() { [5] = RepositoryAccessLevel.Administer } });

        _repositoryService.Received(1).Create(Arg.Is<RepositoryModel>(r => r.Users.Single().Id == 5 && r.GetUserAccessLevel(5) == RepositoryAccessLevel.Administer));
    }

    [TestMethod]
    public void Clone_SourceName_IsBoundFromRoute()
    {
        var method = typeof(RepositoriesController).GetMethods()
            .Single(m => m.Name == nameof(RepositoriesController.Clone) && m.GetParameters().Length == 2);

        Assert.IsTrue(method.GetParameters()[0].IsDefined(typeof(FromRouteAttribute), false));
    }

    [TestMethod]
    public async Task Clone_CopiesSourceAndStoresMembers()
    {
        _repositoryService.GetRepository("source").Returns(new RepositoryModel { Name = "source" });

        var result = await Controller.Clone("source", new RepositoryDetailModel { Name = "copy", PostedSelectedUsers = [1, 2], UserAccessLevels = new() { [1] = RepositoryAccessLevel.Administer } });

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        await _repositoryCopier.Received(1).CopyAsync(Path.Combine(_repositoriesPath, "source"), Path.Combine(_repositoriesPath, "copy"));
        _repositoryService.Received(1).Create(Arg.Is<RepositoryModel>(r => r.Name == "copy" && r.Users.Length == 2 && r.GetUserAccessLevel(1) == RepositoryAccessLevel.Administer && r.GetUserAccessLevel(2) == RepositoryAccessLevel.Push));
    }

    [TestMethod]
    public async Task Clone_CopyFails_DoesNotCreateRepository()
    {
        _repositoryService.GetRepository("source").Returns(new RepositoryModel { Name = "source" });
        _repositoryCopier.CopyAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await Controller.Clone("source", new RepositoryDetailModel { Name = "copy" });

        Assert.IsInstanceOfType<ViewResult>(result);
        _repositoryService.DidNotReceive().Create(Arg.Any<RepositoryModel>());
    }

    [TestMethod]
    public async Task Create_InvalidModel_ShowsSelectionAgain()
    {
        _repositoryService.NameIsUnique("taken", Arg.Any<int>()).Returns(false);

        var result = await Controller.Create(new RepositoryDetailModel { Name = "taken", PostedSelectedUsers = [2] }) as ViewResult;

        Assert.IsNotNull(result);
        var model = (RepositoryDetailModel)result.Model!;
        Assert.AreEqual(2, model.Users.Single().Id);
        _repositoryService.DidNotReceive().Create(Arg.Any<RepositoryModel>());
    }
}
