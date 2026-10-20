using static Gibbon.Git.Server.Integration.Tests.Helper.RepositoryTestHelper;

namespace Gibbon.Git.Server.Integration.Tests.Controller;

[TestClass]
internal class RepositoriesClonePanelTests : IntegrationTestBase
{
    [TestMethod]
    public async Task DetailAndTree_ShowClonePanelWithDefaultTools()
    {
        var client = await CreateClientAsync(true);
        var name = await CreateRepository(client);
        PushCommit(TestContext, GetRepositoryPath(TestContext, name), "master", "content", "readme.txt");
        var encodedUrl = Uri.EscapeDataString($"http://admin@localhost/{name}.git");

        foreach (var page in new[] { "Detail", "Tree" })
        {
            var content = await client.GetStringAsync($"/Repositories/{name}/{page}");

            Assert.Contains("class=\"clone-panel\"", content, page);
            Assert.Contains($"href=\"vscode://vscode.git/clone?url={encodedUrl}\"", content, page);
            Assert.Contains($"jetbrains://rd/checkout/git?checkout.repo={encodedUrl}&amp;idea.required.plugins.id=Git4Idea", content, page);
            Assert.Contains($"data-clipboard-text=\"git clone http://admin@localhost/{name}.git\"", content, page);
            Assert.DoesNotContain("gittower://", content, page);
        }
    }

    [TestMethod]
    public async Task ServerSettings_SelectToolsAndAddCustom_PanelFollowsSettings()
    {
        var client = await CreateClientAsync(true);
        var name = await CreateRepository(client);

        var response = await PostServerSettings(client, ("EnabledCloneTools", ""), ("EnabledCloneTools", "tower"), ("CustomCloneTools", "MyTool|mytool://clone?url={url}"));
        var content = await client.GetStringAsync($"/Repositories/{name}/Detail");
        var settings = await client.GetStringAsync("/Server/Settings");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("gittower://openRepo/", content);
        Assert.Contains("href=\"mytool://clone?url=", content);
        Assert.Contains(">MyTool</a>", content);
        Assert.DoesNotContain("vscode://", content);
        Assert.Contains("""value="tower" checked="checked" />""", settings);
        Assert.Contains("""value="vscode" />""", settings);
    }

    [TestMethod]
    public async Task ServerSettings_InvalidCustomTool_ShowsErrorAndKeepsSettings()
    {
        var client = await CreateClientAsync(true);
        var name = await CreateRepository(client);

        var response = await PostServerSettings(client, ("EnabledCloneTools", ""), ("CustomCloneTools", "Bad|javascript:alert({url})"));
        var content = await response.Content.ReadAsStringAsync();
        var detail = await client.GetStringAsync($"/Repositories/{name}/Detail");

        Assert.Contains("line 1", content);
        Assert.DoesNotContain("javascript:alert", detail);
        Assert.Contains("vscode://", detail);
    }

    [TestMethod]
    public async Task PreferredTool_IsShownAsMainButton()
    {
        var client = await CreateClientAsync(true);
        var name = await CreateRepository(client);
        var postData = new Dictionary<string, string>(await GetAntiForgeryToken(client, "/Account/Settings"))
        {
            { "PreferredCloneTool", "rider" }
        };
        await client.PostAsync("/Account/Settings", new FormUrlEncodedContent(postData));

        var content = await client.GetStringAsync($"/Repositories/{name}/Detail");

        Assert.MatchesRegex("""class="pure-button pure-button-primary clone-preferred" href="jetbrains://rd/checkout/git[^"]*">Open in Rider</a>""", content);
    }

    [TestMethod]
    public async Task EmptyRepository_TreeShowsPushInstructions()
    {
        var client = await CreateClientAsync(true);
        var name = await CreateRepository(client);

        var content = await client.GetStringAsync($"/Repositories/{name}/Tree");

        Assert.Contains("This repository is empty.", content);
        Assert.Contains($"git remote add origin http://admin@localhost/{name}.git", content);
        Assert.DoesNotContain("id=\"files\"", content);
    }

    private static async Task<HttpResponseMessage> PostServerSettings(HttpClient client, params (string Key, string Value)[] fields)
    {
        var postData = new List<KeyValuePair<string, string>>(await GetAntiForgeryToken(client, "/Server/Settings"))
        {
            new("DefaultLanguage", "en")
        };
        foreach (var (key, value) in fields)
        {
            postData.Add(new KeyValuePair<string, string>(key, value));
        }

        return await client.PostAsync("/Server/Settings", new FormUrlEncodedContent(postData));
    }
}
