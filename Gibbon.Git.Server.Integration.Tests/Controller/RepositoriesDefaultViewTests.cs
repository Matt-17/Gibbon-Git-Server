using static Gibbon.Git.Server.Integration.Tests.Helper.RepositoryTestHelper;

namespace Gibbon.Git.Server.Integration.Tests.Controller;

[TestClass]
internal class RepositoriesDefaultViewTests : IntegrationTestBase
{
    [TestMethod]
    public async Task WithoutSettings_RepositoryOpensDetail()
    {
        var client = await CreateClientAsync(true, allowRedirect: false);
        var name = await CreateRepository(client);

        var response = await client.GetAsync($"/Repositories/{name}");

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.AreEqual($"/Repositories/{name}/Detail", response.Headers.Location!.OriginalString);
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync($"/Repositories/{name}/Detail")).StatusCode);
    }

    [TestMethod]
    public async Task ServerDefault_IsUsed_AndUserSettingOverridesIt()
    {
        var client = await CreateClientAsync(true, allowRedirect: false);
        var name = await CreateRepository(client);

        await PostForm(client, "/Server/Settings", ("DefaultLanguage", "en"), ("DefaultRepositoryView", "1"));
        var serverDefault = await client.GetAsync($"/Repositories/{name}");

        await PostForm(client, "/Account/Settings", ("DefaultRepositoryView", "2"));
        var userSetting = await client.GetAsync($"/Repositories/{name}");

        Assert.AreEqual($"/Repositories/{name}/Tree", serverDefault.Headers.Location!.OriginalString);
        Assert.AreEqual($"/Repositories/{name}/Commits", userSetting.Headers.Location!.OriginalString);
    }

    [TestMethod]
    public async Task RepositoryList_LinksToRepositoryStartPage()
    {
        var client = await CreateClientAsync(true);
        var name = await CreateRepository(client);

        var content = await client.GetStringAsync("/Repositories");

        Assert.Contains($"href=\"/Repositories/{name}\"", content);
        Assert.DoesNotContain($"href=\"/Repositories/{name}/Detail\"", content);
    }

    private static async Task PostForm(HttpClient client, string uri, params (string Key, string Value)[] fields)
    {
        var postData = new Dictionary<string, string>(await GetAntiForgeryToken(client, uri));
        foreach (var (key, value) in fields)
        {
            postData[key] = value;
        }

        var response = await client.PostAsync(uri, new FormUrlEncodedContent(postData));
        Assert.IsTrue(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect, $"{uri}: {response.StatusCode}");
    }
}
