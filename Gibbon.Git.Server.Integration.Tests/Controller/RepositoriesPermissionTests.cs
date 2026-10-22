using System.Linq;
using System.Text;

using static Gibbon.Git.Server.Integration.Tests.Helper.RepositoryTestHelper;

namespace Gibbon.Git.Server.Integration.Tests.Controller;

[TestClass]
internal class RepositoriesPermissionTests : IntegrationTestBase
{
    private const string ReaderPassword = "Reader123!";

    [TestMethod]
    public async Task PullOnlyUser_CanBrowseAllReadOnlyPages()
    {
        var admin = await CreateClientAsync(true);
        var name = await CreateRepository(admin, ("AllowAnonymous", "true"));
        var repositoryPath = GetRepositoryPath(TestContext, name);
        PushCommit(TestContext, repositoryPath, GetDefaultBranch(repositoryPath), "readable content", "readme.txt");
        var commit = RunGit(repositoryPath, "rev-parse", "HEAD").Trim();
        var reader = await CreateReaderClient(admin);

        string[] pages =
        [
            "Detail", "Tree", "Blob/readme.txt", "Raw/readme.txt", "Blame/readme.txt", "History/readme.txt",
            "Download", "Tags", "Commits", $"Commit/{commit}"
        ];
        foreach (var page in pages)
        {
            var response = await reader.GetAsync($"/Repositories/{name}/{page}");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, page);
        }
        Assert.AreEqual(HttpStatusCode.Redirect, (await reader.GetAsync($"/Repositories/{name}")).StatusCode);
    }

    [TestMethod]
    public async Task PullOnlyUser_CannotAdministerRepository()
    {
        var admin = await CreateClientAsync(true);
        var name = await CreateRepository(admin, ("AllowAnonymous", "true"));
        var reader = await CreateReaderClient(admin);

        foreach (var page in new[] { "Edit", "Delete" })
        {
            var response = await reader.GetAsync($"/Repositories/{name}/{page}");
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, page);
        }
    }

    [TestMethod]
    public async Task UserWithoutAccess_CannotBrowsePrivateRepository()
    {
        var admin = await CreateClientAsync(true);
        var name = await CreateRepository(admin);
        PushCommit(TestContext, GetRepositoryPath(TestContext, name), GetDefaultBranch(GetRepositoryPath(TestContext, name)), "secret", "readme.txt");
        var reader = await CreateReaderClient(admin);

        foreach (var page in new[] { "", "Detail", "Tree", "Raw/readme.txt", "Commits", "Download", "Clone" })
        {
            var response = await reader.GetAsync($"/Repositories/{name}/{page}");
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, page);
        }
    }

    [TestMethod]
    public async Task AnonymousPullRepository_PushAsksForCredentials()
    {
        var admin = await CreateClientAsync(true);
        var name = await CreateRepository(admin, ("AllowAnonymous", "true"), ("AllowAnonymousPush", "No"));
        var git = Application.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var anonymousPull = await git.GetAsync($"/{name}.git/info/refs?service=git-upload-pack");
        var anonymousPush = await git.GetAsync($"/{name}.git/info/refs?service=git-receive-pack");
        var authenticatedPush = new HttpRequestMessage(HttpMethod.Get, $"/{name}.git/info/refs?service=git-receive-pack");
        authenticatedPush.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String("admin:admin"u8.ToArray()));
        var adminPush = await git.SendAsync(authenticatedPush);

        Assert.AreEqual(HttpStatusCode.OK, anonymousPull.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, anonymousPush.StatusCode);
        Assert.IsTrue(anonymousPush.Headers.WwwAuthenticate.Any(h => h.Scheme == "Basic"));
        Assert.AreEqual(HttpStatusCode.OK, adminPush.StatusCode);
    }

    [TestMethod]
    public async Task AnonymousPullRepository_PushOfNonMemberIsForbidden()
    {
        var admin = await CreateClientAsync(true);
        var name = await CreateRepository(admin, ("AllowAnonymous", "true"), ("AllowAnonymousPush", "No"));
        var (username, password) = await CreateReader(admin);
        var git = Application.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var request = new HttpRequestMessage(HttpMethod.Get, $"/{name}.git/info/refs?service=git-receive-pack");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));

        var response = await git.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<HttpClient> CreateReaderClient(HttpClient admin)
    {
        var (username, password) = await CreateReader(admin);
        var reader = Application.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await AuthenticateUserAsync(reader, username, password);

        return reader;
    }

    private static async Task<(string Username, string Password)> CreateReader(HttpClient admin)
    {
        var username = "reader" + Guid.NewGuid().ToString("N")[..6];
        var postData = new Dictionary<string, string>(await GetAntiForgeryToken(admin, "/Users/Create"))
        {
            { "Username", username },
            { "Name", "Read" },
            { "Surname", "Only" },
            { "Email", $"{username}@example.com" },
            { "Password", ReaderPassword },
            { "ConfirmPassword", ReaderPassword }
        };
        var response = await admin.PostAsync("/Users/Create", new FormUrlEncodedContent(postData));
        response.EnsureSuccessStatusCode();

        return (username, ReaderPassword);
    }
}
