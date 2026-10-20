using System.IO;
using System.Threading.Tasks;

using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Git.GitVersionService;
using Gibbon.Git.Server.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Gibbon.Git.Server.Tests.Git;

[TestClass]
public class GitVersionStartupServiceTests
{
    private IPathResolver _pathResolver = null!;
    private GitVersionService _gitVersionService = null!;

    [TestInitialize]
    public void Setup()
    {
        _pathResolver = Substitute.For<IPathResolver>();
        _pathResolver.GetRepositories().Returns(Path.GetTempPath());
        _pathResolver.ResolveExecutable(Arg.Any<string>()).Returns(x => x.Arg<string>());
        _gitVersionService = new GitVersionService();
    }

    [TestMethod]
    [TestCategory(TestCategories.IntegrationTest)]
    public async Task RunAsync_GitOnPath_MarksGitAvailable()
    {
        await CreateService("git").RunAsync();

        Assert.IsTrue(_gitVersionService.IsGitAvailable);
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    public async Task RunAsync_MissingExecutable_MarksGitUnavailable()
    {
        await CreateService("gibbon-no-such-git").RunAsync();

        Assert.IsFalse(_gitVersionService.IsGitAvailable);
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    public async Task RunAsync_UsesResolvedExecutable()
    {
        await CreateService("tools/git").RunAsync();

        _pathResolver.Received().ResolveExecutable("tools/git");
    }

    private GitVersionStartupService CreateService(string executable)
    {
        var options = Options.Create(new GitSettings { Executable = executable });
        return new GitVersionStartupService(NullLogger<GitVersionStartupService>.Instance, new ProcessService(), _pathResolver, _gitVersionService, options);
    }
}
