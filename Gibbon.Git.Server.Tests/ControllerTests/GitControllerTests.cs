using System;
using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Controllers;
using Gibbon.Git.Server.Data;
using Gibbon.Git.Server.Git.GitService;
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
public class GitControllerTests : TestBase
{
    private GitController _controller = null!;
    private ILogger<GitController> _logger = null!;
    private IRepositoryPermissionService _repositoryPermissionService = null!;
    private IRepositoryService _repositoryService = null!;
    private IUserService _userService = null!;
    private IGitService _gitService = null!;
    private ServerSettings _serverSettings = null!;
    private IPathResolver _pathResolver = null!;

    protected override void ConfigureServices(ServiceCollection services)
    {
        // Empty - all services configured in ConfigureServicesBase
    }

    protected override void ConfigureServicesBase(ServiceCollection services)
    {
        _logger = Substitute.For<ILogger<GitController>>();
        _repositoryPermissionService = Substitute.For<IRepositoryPermissionService>();
        _repositoryService = Substitute.For<IRepositoryService>();
        _userService = Substitute.For<IUserService>();
        _gitService = Substitute.For<IGitService>();
        _serverSettings = new ServerSettings();
        _pathResolver = Substitute.For<IPathResolver>();

        services.AddSingleton(_logger);
        services.AddSingleton(_repositoryPermissionService);
        services.AddSingleton(_repositoryService);
        services.AddSingleton(_userService);
        services.AddSingleton(_gitService);
        services.AddSingleton(_serverSettings);
        services.AddSingleton(_pathResolver);
    }

    protected override void UseServices(IServiceProvider serviceProvider)
    {
        _controller = new GitController(
            _logger,
            _repositoryPermissionService,
            _repositoryService,
            _userService,
            _gitService,
            _serverSettings,
            _pathResolver
        );

        var httpContext = new DefaultHttpContext();
        httpContext.Request.PathBase = new PathString("");
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [TestMethod]
    public void RedirectInfoRefs_WithoutGitExtension_RedirectsToGitUrl()
    {
        // Arrange
        var repositoryName = "test-repo";
        var queryString = "?service=git-upload-pack";
        _controller.HttpContext.Request.QueryString = new QueryString(queryString);

        // Act
        var result = _controller.RedirectInfoRefs(repositoryName);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RedirectResult));
        var redirectResult = (RedirectResult)result;
        Assert.AreEqual($"/{repositoryName}.git/info/refs{queryString}", redirectResult.Url);
    }

    [TestMethod]
    public void RedirectInfoRefs_WithoutQueryString_RedirectsToGitUrl()
    {
        // Arrange
        var repositoryName = "test-repo";
        _controller.HttpContext.Request.QueryString = new QueryString();

        // Act
        var result = _controller.RedirectInfoRefs(repositoryName);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RedirectResult));
        var redirectResult = (RedirectResult)result;
        Assert.AreEqual($"/{repositoryName}.git/info/refs", redirectResult.Url);
    }

    [TestMethod]
    public void RedirectUploadPack_WithoutGitExtension_RedirectsToGitUrl()
    {
        // Arrange
        var repositoryName = "test-repo";

        // Act
        var result = _controller.RedirectUploadPack(repositoryName);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RedirectResult));
        var redirectResult = (RedirectResult)result;
        Assert.AreEqual($"/{repositoryName}.git/git-upload-pack", redirectResult.Url);
        Assert.IsTrue(redirectResult.PreserveMethod);
    }

    [TestMethod]
    public void RedirectReceivePack_WithoutGitExtension_RedirectsToGitUrl()
    {
        // Arrange
        var repositoryName = "test-repo";

        // Act
        var result = _controller.RedirectReceivePack(repositoryName);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RedirectResult));
        var redirectResult = (RedirectResult)result;
        Assert.AreEqual($"/{repositoryName}.git/git-receive-pack", redirectResult.Url);
        Assert.IsTrue(redirectResult.PreserveMethod);
    }

    [TestMethod]
    public void RedirectInfoRefs_WithPathBase_IncludesPathBaseInRedirect()
    {
        // Arrange
        var repositoryName = "test-repo";
        var pathBase = "/git";
        var queryString = "?service=git-upload-pack";
        _controller.HttpContext.Request.PathBase = new PathString(pathBase);
        _controller.HttpContext.Request.QueryString = new QueryString(queryString);

        // Act
        var result = _controller.RedirectInfoRefs(repositoryName);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RedirectResult));
        var redirectResult = (RedirectResult)result;
        Assert.AreEqual($"{pathBase}/{repositoryName}.git/info/refs{queryString}", redirectResult.Url);
    }
}
