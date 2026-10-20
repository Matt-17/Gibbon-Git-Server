namespace Gibbon.Git.Server.Integration.Tests.Controller;

internal class IntegrationTestBase
{
    protected GibbonGitServerApplication Application { get; set; } = null!;

    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public async Task Setup()
    {
        Application = new GibbonGitServerApplication(TestContext);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Application.Dispose();
    }

    public async Task<HttpClient> CreateClientAsync(bool authenticated, bool allowRedirect = true)
    {
        WebApplicationFactoryClientOptions options = new()
        {
            AllowAutoRedirect = allowRedirect
        };
        var client = Application.CreateClient(options);
        if (authenticated)
        {
            await AuthenticateUserAsync(client);
        }
        return client;
    }

    protected static async Task AuthenticateUserAsync(HttpClient client, string username = "admin", string password = "admin")
    {
        const string requestUri = "/Login";
        var loginData = new Dictionary<string, string>(await GetAntiForgeryToken(client, requestUri))
        {
            { "Username", username },
            { "Password", password }
        };

        var loginContent = new FormUrlEncodedContent(loginData);

        _ = await client.PostAsync(requestUri, loginContent);
    }

}
