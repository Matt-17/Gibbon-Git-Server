using System;
using System.IO;
using System.Linq;

using System.Threading.Tasks;

using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Repositories;
using Gibbon.Git.Server.Services;

using LibGit2Sharp;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Gibbon.Git.Server.Tests.Repositories;

[TestClass]
[TestCategory(TestCategories.UnitTest)]
public class RepositoryCopierTests
{
    private static readonly Signature Author = new("Test", "test@example.com", DateTimeOffset.Now);

    private string _directory = null!;
    private string _source = null!;
    private string _target = null!;
    private RepositoryCopier _copier = null!;

    [TestInitialize]
    public void Setup()
    {
        _directory = Path.Combine(Path.GetTempPath(), "gibbon-copy-" + Guid.NewGuid().ToString("N"));
        _source = Path.Combine(_directory, "source");
        _target = Path.Combine(_directory, "target");
        Repository.Init(_source, isBare: true);

        var pathResolver = Substitute.For<IPathResolver>();
        pathResolver.ResolveExecutable(Arg.Any<string>()).Returns(x => x.Arg<string>());
        pathResolver.ResolveRoot(Arg.Any<string>()).Returns(_directory);
        pathResolver.GetRepositories().Returns(_directory);
        _copier = new RepositoryCopier(NullLogger<RepositoryCopier>.Instance, Options.Create(new GitSettings()), new ProcessService(), pathResolver);
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
    public async Task Copy_TransfersBranchesTagsAndHead()
    {
        using (var source = new Repository(_source))
        {
            var first = Commit(source, "first", null);
            var second = Commit(source, "second", first);
            source.Refs.Add("refs/heads/main", second.Id);
            source.Refs.Add("refs/heads/feature/x", first.Id);
            source.Tags.Add("v1.0", first);
            source.Tags.Add("v2.0", second, Author, "release");
            source.Refs.UpdateTarget(source.Refs.Head, source.Refs["refs/heads/main"]);
        }

        Assert.IsTrue(await _copier.CopyAsync(_source, _target));

        using var source2 = new Repository(_source);
        using var target = new Repository(_target);
        Assert.IsTrue(target.Info.IsBare);
        CollectionAssert.AreEquivalent(new[] { "feature/x", "main" }, target.Branches.Select(b => b.FriendlyName).ToArray());
        CollectionAssert.AreEquivalent(new[] { "v1.0", "v2.0" }, target.Tags.Select(t => t.FriendlyName).ToArray());
        Assert.AreEqual(source2.Branches["main"].Tip.Sha, target.Branches["main"].Tip.Sha);
        Assert.IsTrue(target.Tags["v2.0"].IsAnnotated);
        Assert.AreEqual("refs/heads/main", target.Refs.Head.TargetIdentifier);
        Assert.AreEqual("second", target.Head.Tip.MessageShort);
    }

    [TestMethod]
    public async Task Copy_DoesNotKeepARemote()
    {
        using (var source = new Repository(_source))
        {
            source.Refs.Add("refs/heads/master", Commit(source, "first", null).Id);
        }

        Assert.IsTrue(await _copier.CopyAsync(_source, _target));

        using var target = new Repository(_target);
        Assert.IsEmpty(target.Network.Remotes);
        Assert.IsNull(target.Config.Get<string>("remote.origin.url"));
        Assert.DoesNotContain("remote", File.ReadAllText(Path.Combine(target.Info.Path, "config")));
    }

    [TestMethod]
    public async Task Copy_EmptyRepository_KeepsDefaultBranchOfSource()
    {
        using (var source = new Repository(_source))
        {
            File.WriteAllText(Path.Combine(source.Info.Path, "HEAD"), "ref: refs/heads/main\n");
        }

        Assert.IsTrue(await _copier.CopyAsync(_source, _target));

        using var target = new Repository(_target);
        Assert.AreEqual("refs/heads/main", target.Refs.Head.TargetIdentifier);
    }

    [TestMethod]
    public async Task Copy_EmptyRepository_CreatesEmptyRepository()
    {
        Assert.IsTrue(await _copier.CopyAsync(_source, _target));

        using var target = new Repository(_target);
        Assert.IsTrue(target.Info.IsBare);
        Assert.IsEmpty(target.Branches);
    }

    [TestMethod]
    public async Task Copy_InvalidSource_FailsAndLeavesNoTarget()
    {
        var result = await _copier.CopyAsync(Path.Combine(_directory, "missing"), _target);

        Assert.IsFalse(result);
        Assert.IsFalse(Directory.Exists(_target));
    }

    private static Commit Commit(Repository repository, string message, Commit? parent)
    {
        var blob = repository.ObjectDatabase.CreateBlob(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(message)));
        var tree = new TreeDefinition();
        tree.Add("file.txt", blob, Mode.NonExecutableFile);
        var parents = parent is null ? Array.Empty<Commit>() : new[] { parent };

        return repository.ObjectDatabase.CreateCommit(Author, Author, message, repository.ObjectDatabase.CreateTree(tree), parents, false);
    }
}
