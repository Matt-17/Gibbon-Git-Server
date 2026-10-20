using System.Diagnostics;
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
        _pathResolver.GetRoot().Returns(Path.GetTempPath());
        _pathResolver.GetRepositories().Returns(Path.Combine(Path.GetTempPath(), "gibbon-missing-repositories"));
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

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    [DataRow("git version 2.43.0\n", "2.43.0")]
    [DataRow("git version 2.47.1.windows.2", "2.47.1")]
    [DataRow("git version 2.39.5 (Apple Git-154)", "2.39.5")]
    [DataRow("git version 2.5.0.1", "2.5.0.1")]
    [DataRow("git version 2.45", "2.45")]
    [DataRow("git version 3", "3")]
    [DataRow("unexpected", "unexpected")]
    [DataRow("", "")]
    [DataRow(null, "")]
    public void StripVersion_ParsesTypicalOutputs(string output, string expected)
    {
        Assert.AreEqual(expected, GitVersionStartupService.StripVersion(output));
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    public async Task RunAsync_UnparsableVersion_StillMarksGitAvailable()
    {
        var processService = Substitute.For<IProcessService>();
        processService.StartProcessAsync(Arg.Any<ProcessStartInfo>()).Returns(new ProcessResult { IsSuccess = true, Output = "git version" });
        var options = Options.Create(new GitSettings { Executable = "git" });

        await new GitVersionStartupService(NullLogger<GitVersionStartupService>.Instance, processService, _pathResolver, _gitVersionService, options).RunAsync();

        Assert.IsTrue(_gitVersionService.IsGitAvailable);
    }

    private GitVersionStartupService CreateService(string executable)
    {
        var options = Options.Create(new GitSettings { Executable = executable });
        return new GitVersionStartupService(NullLogger<GitVersionStartupService>.Instance, new ProcessService(), _pathResolver, _gitVersionService, options);
    }
}
