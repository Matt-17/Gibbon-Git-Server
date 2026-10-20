using static Gibbon.Git.Server.Integration.Tests.Helper.RepositoryTestHelper;

namespace Gibbon.Git.Server.Integration.Tests.Controller;

[TestClass]
internal class RepositoriesCommitsTests : IntegrationTestBase
{
    [TestMethod]
    [DataRow("Commits")]
    [DataRow("Tree")]
    [DataRow("Tags")]
    public async Task EmptyRepository_PagesRender(string page)
    {
        var client = await CreateClientAsync(true);
        var name = await CreateRepository(client);

        var response = await client.GetAsync($"/Repositories/{name}/{page}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task EmptyRepository_CommitsShowsHint()
    {
        var client = await CreateClientAsync(true);
        var name = await CreateRepository(client);

        var content = await client.GetStringAsync($"/Repositories/{name}/Commits");

        Assert.Contains("No commits found", content);
    }

    [TestMethod]
    public async Task DefaultBranchOtherThanMaster_CommitsShowsHeadBranch()
    {
        var client = await CreateClientAsync(true);
        var name = await CreateRepository(client);
        var repositoryPath = GetRepositoryPath(TestContext, name);
        RunGit(repositoryPath, "symbolic-ref", "HEAD", "refs/heads/main");
        PushCommit(TestContext, repositoryPath, "main", "first commit on main");

        var response = await client.GetAsync($"/Repositories/{name}/Commits");
        var content = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("first commit on main", content);
        Assert.MatchesRegex($"""<span class="tag">\s*<a href="/Repositories/{name}/Commits\?branch=v1">v1</a>""", content);
    }

    [TestMethod]
    public async Task TagsPage_LinksContainRepositoryName()
    {
        var client = await CreateClientAsync(true);
        var name = await CreateRepository(client);
        PushCommit(TestContext, GetRepositoryPath(TestContext, name), "master", "tagged");

        var content = await client.GetStringAsync($"/Repositories/{name}/Tags");

        Assert.Contains($"href=\"/Repositories/{name}/Commits?branch=v1\"", content);
        Assert.Contains($"href=\"/Repositories/{name}/Commit/", content);
        Assert.DoesNotContain("href=\"/Repositories/Commit", content);
    }
}
