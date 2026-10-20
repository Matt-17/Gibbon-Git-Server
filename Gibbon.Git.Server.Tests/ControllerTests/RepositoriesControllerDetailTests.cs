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

    protected override void ConfigureServices(ServiceCollection services)
    {
        services.AddSubstitute<ILogger<RepositoriesController>>();
        services.AddSubstitute<ITeamService>();
        _repositoryService = services.AddSubstitute<IRepositoryService>();
        services.AddSubstitute<IUserService>();
        _permissionService = services.AddSubstitute<IRepositoryPermissionService>();
        services.AddSubstitute<IRepositorySynchronizer>();
        services.AddSingleton(new ServerSettings());
        _pathResolver = services.AddSubstitute<IPathResolver>();
        _browserFactory = services.AddSubstitute<IRepositoryBrowserFactory>();
        services.AddSubstitute<IRepositoryLogoService>();
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

}
