using System.Threading.Tasks;

using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Controllers;
using Gibbon.Git.Server.Data;
using Gibbon.Git.Server.Models;
using Gibbon.Git.Server.Repositories;
using Gibbon.Git.Server.Security;
using Gibbon.Git.Server.Services;
using Gibbon.Git.Server.Tests.TestHelper;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace Gibbon.Git.Server.Tests.ControllerTests;

[TestClass]
public class RepositoriesControllerDetailTests : ControllerTestBase<RepositoriesController>
{
    private IRepositoryService _repositoryService = null!;
    private IRepositoryPermissionService _permissionService = null!;
    private IRepositoryBrowserFactory _browserFactory = null!;
    private IPathResolver _pathResolver = null!;
    private IUserSettingsService _userSettingsService = null!;
    private readonly ServerSettings _serverSettings = new();

    protected override void ConfigureServices(ServiceCollection services)
    {
        services.AddSubstitute<ILogger<RepositoriesController>>();
        services.AddSubstitute<ITeamService>();
        _repositoryService = services.AddSubstitute<IRepositoryService>();
        services.AddSubstitute<IUserService>();
        _permissionService = services.AddSubstitute<IRepositoryPermissionService>();
        services.AddSubstitute<IRepositorySynchronizer>();
        services.AddSingleton(_serverSettings);
        _pathResolver = services.AddSubstitute<IPathResolver>();
        _browserFactory = services.AddSubstitute<IRepositoryBrowserFactory>();
        services.AddSubstitute<IRepositoryLogoService>();
        services.AddSubstitute<IRepositoryCopier>();
        services.AddSubstitute<IRepositoryHeadService>();
        _userSettingsService = services.AddSubstitute<IUserSettingsService>();
        _userSettingsService.GetSettings(Arg.Any<int>()).Returns(new UserSettings());
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    public void Detail_RepositoryNotFound_ReturnsViewWithNullModel()
    {
        _repositoryService.GetRepository(Arg.Any<string>()).Returns((RepositoryModel)null!);

        var result = Controller.Detail("missing-repo") as ViewResult;

        Assert.IsNotNull(result);
        Assert.IsNull(result.Model);
        _browserFactory.DidNotReceive().Create(Arg.Any<string>());
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    public async Task Open_NoSettings_RedirectsToDetail()
    {
        var result = await Controller.Open("demo") as RedirectToActionResult;

        Assert.AreEqual(nameof(RepositoriesController.Detail), result!.ActionName);
        Assert.AreEqual("demo", result.RouteValues!["name"]);
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    [DataRow(RepositoryView.Tree, nameof(RepositoriesController.Tree))]
    [DataRow(RepositoryView.Commits, nameof(RepositoriesController.Commits))]
    [DataRow(RepositoryView.Tags, nameof(RepositoriesController.Tags))]
    public async Task Open_ServerDefault_RedirectsToConfiguredView(RepositoryView view, string action)
    {
        _serverSettings.DefaultRepositoryView = view;

        var result = await Controller.Open("demo") as RedirectToActionResult;

        Assert.AreEqual(action, result!.ActionName);
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    public async Task Open_UserSetting_OverridesServerDefault()
    {
        _serverSettings.DefaultRepositoryView = RepositoryView.Tree;
        _userSettingsService.GetSettings(Arg.Any<int>()).Returns(new UserSettings { DefaultRepositoryView = RepositoryView.Detail });

        var result = await Controller.Open("demo") as RedirectToActionResult;

        Assert.AreEqual(nameof(RepositoriesController.Detail), result!.ActionName);
    }

}
