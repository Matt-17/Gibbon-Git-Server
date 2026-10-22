using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Repositories;
using Gibbon.Git.Server.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Gibbon.Git.Server.Tests.Repositories;

[TestClass]
[TestCategory(TestCategories.UnitTest)]
public class RepositoryHeadServiceTests
{
    private const string Name = "repo";

    private string _directory = null!;
    private string _repository = null!;
    private RepositoryHeadService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _directory = Path.Combine(Path.GetTempPath(), "gibbon-head-" + Guid.NewGuid().ToString("N"));
        _repository = Path.Combine(_directory, Name);
        Directory.CreateDirectory(_directory);
        Git(_directory, "init", "-q", "--bare", "--initial-branch=initial", _repository);

        var pathResolver = Substitute.For<IPathResolver>();
        pathResolver.ResolveExecutable(Arg.Any<string>()).Returns(x => x.Arg<string>());
        pathResolver.ResolveRoot(Arg.Any<string>()).Returns(_directory);
        pathResolver.GetRepositories().Returns(_directory);
        pathResolver.GetRepositoryPath(Arg.Any<string>()).Returns(x => Path.Combine(_directory, x.Arg<string>()));
        _service = new RepositoryHeadService(NullLogger<RepositoryHeadService>.Instance, Options.Create(new GitSettings()), new ProcessService(), pathResolver);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (!Directory.Exists(_directory))
        {
            return;
        }
        foreach (var file in Directory.EnumerateFiles(_directory, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }
        Directory.Delete(_directory, true);
    }

    [TestMethod]
    public async Task Repair_OnlyOtherBranchPushed_PointsHeadToIt()
    {
        Push("trunk", "2024-01-01T10:00:00");

        Assert.IsTrue(await _service.RepairHeadAsync(Name));

        Assert.AreEqual("refs/heads/trunk", Head());
    }

    [TestMethod]
    public async Task Repair_SeveralBranches_PointsHeadToNewestCommit()
    {
        Push("older", "2024-01-01T10:00:00");
        Push("newer", "2024-06-01T10:00:00");

        Assert.IsTrue(await _service.RepairHeadAsync(Name));

        Assert.AreEqual("refs/heads/newer", Head());
    }

    [TestMethod]
    public async Task Repair_HeadBranchExists_KeepsHead()
    {
        Push("initial", "2024-01-01T10:00:00");
        Push("newer", "2024-06-01T10:00:00");

        Assert.IsFalse(await _service.RepairHeadAsync(Name));

        Assert.AreEqual("refs/heads/initial", Head());
    }

    [TestMethod]
    public async Task Repair_EmptyRepository_KeepsHead()
    {
        Assert.IsFalse(await _service.RepairHeadAsync(Name));

        Assert.AreEqual("refs/heads/initial", Head());
    }

    [TestMethod]
    public async Task Init_PointsHeadToGivenBranch()
    {
        Assert.IsTrue(await _service.InitAsync("created", "trunk"));

        Assert.AreEqual("refs/heads/trunk", Git(Path.Combine(_directory, "created"), "symbolic-ref", "HEAD").Trim());
    }

    [TestMethod]
    public async Task GetBranches_ReturnsHeadAndSortedBranches()
    {
        Push("initial", "2024-01-01T10:00:00");
        Push("develop", "2024-06-01T10:00:00");

        var branches = await _service.GetBranchesAsync(Name);

        Assert.AreEqual("initial", branches.DefaultBranch);
        CollectionAssert.AreEqual(new[] { "develop", "initial" }, branches.Branches.ToArray());
    }

    [TestMethod]
    public async Task SetDefaultBranch_ExistingBranch_ChangesHead()
    {
        Push("initial", "2024-01-01T10:00:00");
        Push("develop", "2024-06-01T10:00:00");

        Assert.IsTrue(await _service.SetDefaultBranchAsync(Name, "develop"));

        Assert.AreEqual("refs/heads/develop", Head());
    }

    [TestMethod]
    public async Task SetDefaultBranch_MissingBranch_KeepsHead()
    {
        Push("initial", "2024-01-01T10:00:00");

        Assert.IsFalse(await _service.SetDefaultBranchAsync(Name, "missing"));

        Assert.AreEqual("refs/heads/initial", Head());
    }

    private string Head() => Git(_repository, "symbolic-ref", "HEAD").Trim();

    private void Push(string branch, string date)
    {
        var work = Path.Combine(_directory, "work-" + Guid.NewGuid().ToString("N"));
        Git(_directory, "init", "-q", "--initial-branch=" + branch, work);
        Git(work, "-c", "user.name=Test", "-c", "user.email=test@example.com", "commit", "-q", "--allow-empty", "-m", branch, "--date", date);
        Git(work, "push", "-q", _repository, branch);
    }

    private static string Git(string workingDirectory, params string[] arguments)
    {
        var info = new ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        // committer date decides which branch is the newest
        var date = Array.IndexOf(arguments, "--date");
        if (date >= 0)
        {
            info.EnvironmentVariables["GIT_COMMITTER_DATE"] = arguments[date + 1];
        }
        using var process = Process.Start(info)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.AreEqual(0, process.ExitCode, error);

        return output;
    }
}
