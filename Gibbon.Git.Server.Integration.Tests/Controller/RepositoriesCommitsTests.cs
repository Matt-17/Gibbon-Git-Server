using System.Diagnostics;

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
        var repositoryPath = Path.Combine(TestContext.TestRunDirectory!, "Databases", "Repositories", name);
        RunGit(repositoryPath, "symbolic-ref", "HEAD", "refs/heads/main");
        PushCommit(repositoryPath, "main", "first commit on main");

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
        PushCommit(Path.Combine(TestContext.TestRunDirectory!, "Databases", "Repositories", name), "master", "tagged");

        var content = await client.GetStringAsync($"/Repositories/{name}/Tags");

        Assert.Contains($"href=\"/Repositories/{name}/Commits?branch=v1\"", content);
        Assert.Contains($"href=\"/Repositories/{name}/Commit/", content);
        Assert.DoesNotContain("href=\"/Repositories/Commit", content);
    }

    private static async Task<string> CreateRepository(HttpClient client)
    {
        var name = "repo" + Guid.NewGuid().ToString("N")[..8];
        var postData = new Dictionary<string, string>(await GetAntiForgeryToken(client, "/Repositories/Create"))
        {
            { "Name", name },
            { "PostedSelectedAdministrators", "1" }
        };
        var response = await client.PostAsync("/Repositories/Create", new FormUrlEncodedContent(postData));
        response.EnsureSuccessStatusCode();

        return name;
    }

    private void PushCommit(string repositoryPath, string branch, string message)
    {
        var workingDirectory = Path.Combine(TestContext.TestRunDirectory!, "work-" + Guid.NewGuid().ToString("N"));
        RunGit(TestContext.TestRunDirectory!, "init", "-q", "-b", branch, workingDirectory);
        RunGit(workingDirectory, "-c", "user.name=Test", "-c", "user.email=test@example.com", "commit", "-q", "--allow-empty", "-m", message);
        RunGit(workingDirectory, "tag", "v1");
        RunGit(workingDirectory, "push", "-q", repositoryPath, branch, "v1");
    }

    private static void RunGit(string workingDirectory, params string[] arguments)
    {
        using var process = Process.Start(new ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            UseShellExecute = false
        })!;
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.AreEqual(0, process.ExitCode, error);
    }
}
