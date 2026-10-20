using System.IO;

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
public class RepositoriesControllerLogoTests : ControllerTestBase<RepositoriesController>
{
    private IRepositoryService _repositoryService = null!;
    private IRepositoryPermissionService _permissionService = null!;
    private IRepositoryLogoService _logoService = null!;

    protected override void ConfigureServices(ServiceCollection services)
    {
        services.AddSubstitute<ILogger<RepositoriesController>>();
        services.AddSubstitute<ITeamService>().GetAllTeams().Returns([]);
        _repositoryService = services.AddSubstitute<IRepositoryService>();
        services.AddSubstitute<IUserService>().GetAllUsers().Returns([]);
        _permissionService = services.AddSubstitute<IRepositoryPermissionService>();
        services.AddSubstitute<IRepositorySynchronizer>();
        services.AddSingleton(new ServerSettings());
        services.AddSubstitute<IPathResolver>();
        services.AddSubstitute<IRepositoryBrowserFactory>();
        _logoService = services.AddSubstitute<IRepositoryLogoService>();
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    public void Create_ValidLogo_StoresProcessedLogo()
    {
        var processed = new byte[] { 1, 2, 3 };
        _logoService.CreateLogo(Arg.Any<Stream>()).Returns(processed);
        var model = CreateModelWithLogo();

        Controller.Create(model);

        Assert.AreSame(processed, model.Logo.BinaryData);
        Assert.IsFalse(Controller.ModelState.ContainsKey("Logo.PostedFile"));
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    public void Create_InvalidLogo_AddsModelErrorAndDoesNotCreate()
    {
        _logoService.CreateLogo(Arg.Any<Stream>()).Returns((byte[])null!);
        _repositoryService.NameIsUnique(Arg.Any<string>(), Arg.Any<int>()).Returns(true);
        var model = CreateModelWithLogo();

        var result = Controller.Create(model) as ViewResult;

        Assert.IsNotNull(result);
        Assert.IsTrue(Controller.ModelState.ContainsKey("Logo.PostedFile"));
        _repositoryService.DidNotReceive().Create(Arg.Any<RepositoryModel>());
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    public void Create_WithoutLogo_DoesNotCallLogoService()
    {
        var model = new RepositoryDetailModel { Name = "repo", Logo = new RepositoryLogoDetailModel() };
        _permissionService.HasCreatePermission(Arg.Any<int>()).Returns(true);

        Controller.Create(model);

        _logoService.DidNotReceive().CreateLogo(Arg.Any<Stream>());
        Assert.IsNull(model.Logo.BinaryData);
    }

    private RepositoryDetailModel CreateModelWithLogo()
    {
        _permissionService.HasCreatePermission(Arg.Any<int>()).Returns(true);
        return new RepositoryDetailModel
        {
            Name = "repo",
            Logo = new RepositoryLogoDetailModel
            {
                PostedFile = new FormFile(new MemoryStream([1, 2, 3]), 0, 3, "Logo.PostedFile", "logo.png")
            }
        };
    }
}
