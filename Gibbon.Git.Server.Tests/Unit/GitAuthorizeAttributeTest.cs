using System.IO;

using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Services;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Gibbon.Git.Server.Tests.Unit;

[TestClass]
public class PathResolverTests
{
    private static readonly char DirectorySeparator = Path.DirectorySeparatorChar;
    private static readonly string RootPath = Path.GetPathRoot(Directory.GetCurrentDirectory()) ?? "/";
    private IPathResolver _pathResolver = null!;

    [TestInitialize]
    public void Initialize()
    {
        IWebHostEnvironment webEnvironment = Substitute.For<IWebHostEnvironment>();
        webEnvironment.ContentRootPath.Returns(RootPath);

        IOptions<ApplicationSettings> configuration = Substitute.For<IOptions<ApplicationSettings>>();
        configuration.Value.Returns(new ApplicationSettings
        {
            RepositoryPath = "test",
            DataPath = $"~{DirectorySeparator}Data"
        });

        _pathResolver = new PathResolver(webEnvironment, configuration);
    }

    [TestMethod]
    public void GetRepoPathTest()
    {
        var repo = _pathResolver.GetRepoPath("/other/test.git/info/refs", "/other");
        Assert.AreEqual("test", repo);

        repo = _pathResolver.GetRepoPath("/test.git/info/refs", "/");
        Assert.AreEqual("test", repo);
    }

    [TestMethod]
    public void ResolveTest()
    {
        var path = _pathResolver.Resolve("test");
        var expected = Path.Combine(RootPath, "Data", "test");
        Assert.AreEqual(expected, path);
    }

    [TestMethod]
    public void ResolveTest_TwoPaths()
    {
        var path = _pathResolver.Resolve("test", "subdir");
        var expected = Path.Combine(RootPath, "Data", "test", "subdir");
        Assert.AreEqual(expected, path);
    }

    [TestMethod]
    [DataRow("App_Data\\Repositories")]
    [DataRow("App_Data/Repositories")]
    public void ResolveRoot_AcceptsBothSeparators(string configuredPath)
    {
        var path = _pathResolver.ResolveRoot(configuredPath);

        Assert.AreEqual(Path.Combine(RootPath, "App_Data", "Repositories"), path);
    }

    [TestMethod]
    [DataRow("git")]
    [DataRow("git.exe")]
    public void ResolveExecutable_CommandName_IsLeftForPathLookup(string executable)
    {
        Assert.AreEqual(executable, _pathResolver.ResolveExecutable(executable));
    }

    [TestMethod]
    [DataRow("tools/git/bin/git")]
    [DataRow("tools\\git\\bin\\git")]
    public void ResolveExecutable_RelativePath_IsResolvedAgainstContentRoot(string executable)
    {
        Assert.AreEqual(Path.Combine(RootPath, "tools", "git", "bin", "git"), _pathResolver.ResolveExecutable(executable));
    }

    [TestMethod]
    public void ResolveExecutable_AbsolutePath_IsUnchanged()
    {
        var executable = Path.Combine(RootPath, "usr", "bin", "git");

        Assert.AreEqual(executable, _pathResolver.ResolveExecutable(executable));
    }
}
