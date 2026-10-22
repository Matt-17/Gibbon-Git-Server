using static Gibbon.Git.Server.Integration.Tests.Helper.RepositoryTestHelper;

namespace Gibbon.Git.Server.Integration.Tests.Controller;

[TestClass]
internal class RepositoriesDefaultBranchTests : IntegrationTestBase
{
    [TestMethod]
    public async Task NewRepository_StartsWithServerDefaultBranch()
    {
        var client = await CreateClientAsync(true, allowRedirect: false);
        await PostServerSettings(client, "trunk");

        var name = await CreateRepository(client);

        Assert.AreEqual("trunk", GetDefaultBranch(GetRepositoryPath(TestContext, name)));
        StringAssert.Contains(await client.GetStringAsync($"/Repositories/{name}/Tree"), "git push -u origin trunk");
    }

    [TestMethod]
    public async Task ServerSettings_InvalidBranchName_IsRejected()
    {
        var client = await CreateClientAsync(true, allowRedirect: false);

        var response = await PostServerSettings(client, "bad name");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "field-validation-error");
        StringAssert.Contains(await client.GetStringAsync("/Server/Settings"), "value=\"main\"");
    }

    [TestMethod]
    public async Task Edit_ChangesDefaultBranchToExistingBranch()
    {
        var client = await CreateClientAsync(true, allowRedirect: false);
        var name = await CreateRepository(client);
        var repositoryPath = GetRepositoryPath(TestContext, name);
        PushCommit(TestContext, repositoryPath, GetDefaultBranch(repositoryPath), "first");
        RunGit(repositoryPath, "branch", "develop", GetDefaultBranch(repositoryPath));

        var edit = await client.GetStringAsync($"/Repositories/{name}/Edit");
        var postData = new Dictionary<string, string>(await GetAntiForgeryToken(client, $"/Repositories/{name}/Edit"))
        {
            { "Id", GetRepositoryId(edit) },
            { "Name", name },
            { "PostedSelectedUsers", "1" },
            { "UserAccessLevels[1]", "Administer" },
            { "DefaultBranch", "develop" }
        };
        var response = await client.PostAsync($"/Repositories/{name}/Edit", new FormUrlEncodedContent(postData));

        StringAssert.Contains(edit, "<option>develop</option>");
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.AreEqual("develop", GetDefaultBranch(repositoryPath));
    }

    private static string GetRepositoryId(string editPage)
    {
        var match = System.Text.RegularExpressions.Regex.Match(editPage, "name=\"Id\" type=\"hidden\" value=\"(\\d+)\"");
        Assert.IsTrue(match.Success, "no repository id on the edit page");
        return match.Groups[1].Value;
    }

    private static async Task<HttpResponseMessage> PostServerSettings(HttpClient client, string defaultBranch)
    {
        var postData = new Dictionary<string, string>(await GetAntiForgeryToken(client, "/Server/Settings"))
        {
            { "DefaultLanguage", "en" },
            { "DefaultBranch", defaultBranch }
        };
        return await client.PostAsync("/Server/Settings", new FormUrlEncodedContent(postData));
    }
}
