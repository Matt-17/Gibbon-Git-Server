using static Gibbon.Git.Server.Integration.Tests.Helper.RepositoryTestHelper;

namespace Gibbon.Git.Server.Integration.Tests.Controller;

[TestClass]
internal class RepositoriesRenameBranchTests : IntegrationTestBase
{
    [TestMethod]
    public async Task RenameDefaultBranch_MovesHeadAndShowsClientCommands()
    {
        var client = await CreateClientAsync(true, allowRedirect: false);
        var (name, repositoryPath) = await CreateRepositoryWithCommit(client);
        var branch = GetDefaultBranch(repositoryPath);

        var response = await RenameBranch(client, name, branch, "renamed");
        var edit = await client.GetStringAsync($"/Repositories/{name}/Edit");

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.AreEqual("renamed", GetDefaultBranch(repositoryPath));
        StringAssert.Contains(edit, "summary-success");
        StringAssert.Contains(edit, $"git branch -m {branch} renamed");
        StringAssert.Contains(edit, "git branch -u origin/renamed renamed");
    }

    [TestMethod]
    public async Task RenameToExistingBranch_ShowsErrorAndKeepsBranches()
    {
        var client = await CreateClientAsync(true, allowRedirect: false);
        var (name, repositoryPath) = await CreateRepositoryWithCommit(client);
        var branch = GetDefaultBranch(repositoryPath);
        RunGit(repositoryPath, "branch", "develop", branch);

        await RenameBranch(client, name, "develop", branch);
        var edit = await client.GetStringAsync($"/Repositories/{name}/Edit");

        StringAssert.Contains(edit, "summary-error");
        StringAssert.Contains(RunGit(repositoryPath, "branch", "--list", "develop"), "develop");
    }

    [TestMethod]
    public async Task RenameToInvalidName_ShowsError()
    {
        var client = await CreateClientAsync(true, allowRedirect: false);
        var (name, repositoryPath) = await CreateRepositoryWithCommit(client);
        var branch = GetDefaultBranch(repositoryPath);

        await RenameBranch(client, name, branch, "bad name");
        var edit = await client.GetStringAsync($"/Repositories/{name}/Edit");

        StringAssert.Contains(edit, "summary-error");
        Assert.AreEqual(branch, GetDefaultBranch(repositoryPath));
    }

    private async Task<(string Name, string Path)> CreateRepositoryWithCommit(HttpClient client)
    {
        var name = await CreateRepository(client);
        var repositoryPath = GetRepositoryPath(TestContext, name);
        PushCommit(TestContext, repositoryPath, GetDefaultBranch(repositoryPath), "first");
        return (name, repositoryPath);
    }

    private static async Task<HttpResponseMessage> RenameBranch(HttpClient client, string name, string branch, string newName)
    {
        var postData = new Dictionary<string, string>(await GetAntiForgeryToken(client, $"/Repositories/{name}/Edit"))
        {
            { "branch", branch },
            { "newName", newName }
        };
        return await client.PostAsync($"/Repositories/{name}/RenameBranch", new FormUrlEncodedContent(postData));
    }
}
