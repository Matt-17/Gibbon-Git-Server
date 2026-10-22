using static Gibbon.Git.Server.Integration.Tests.Helper.RepositoryTestHelper;

namespace Gibbon.Git.Server.Integration.Tests.Controller;

[TestClass]
internal class RepositoryNameCaseTests : IntegrationTestBase
{
    // the in-memory provider compares case-sensitively, the real databases do not
    protected override string DatabaseProvider => "Sqlite";

    [TestMethod]
    public async Task GitUrl_WithDifferentCase_ServesRepository()
    {
        var admin = await CreateClientAsync(true);
        var name = await CreateRepository(admin, ("AllowAnonymous", "true"));
        var repositoryPath = GetRepositoryPath(TestContext, name);
        PushCommit(TestContext, repositoryPath, GetDefaultBranch(repositoryPath), "case test", "readme.txt");
        var commit = RunGit(repositoryPath, "rev-parse", "HEAD").Trim();
        var git = Application.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await git.GetAsync($"/{name.ToUpperInvariant()}.git/info/refs?service=git-upload-pack");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), commit);
    }

    [TestMethod]
    public async Task WebUrl_WithDifferentCase_RedirectsTemporarily()
    {
        var admin = await CreateClientAsync(true, allowRedirect: false);
        var name = await CreateRepository(admin);

        var response = await admin.GetAsync($"/Repositories/{name.ToUpperInvariant()}/Commits");

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        StringAssert.EndsWith(response.Headers.Location!.ToString(), $"/Repositories/{name}/Commits");
    }
}
