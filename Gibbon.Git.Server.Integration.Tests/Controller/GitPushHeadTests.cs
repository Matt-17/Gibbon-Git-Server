using System.Linq;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

using static Gibbon.Git.Server.Integration.Tests.Helper.RepositoryTestHelper;

namespace Gibbon.Git.Server.Integration.Tests.Controller;

[TestClass]
internal class GitPushHeadTests : IntegrationTestBase
{
    [TestMethod]
    public async Task PushOfOtherBranchOnly_PointsHeadToIt()
    {
        // git needs a real HTTP server
        Application.UseKestrel(0);
        Application.StartServer();
        var address = Application.Services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>().Addresses.First();
        Application.ClientOptions.BaseAddress = new Uri(address.Replace("127.0.0.1", "localhost"));
        var admin = await CreateClientAsync(true);
        var name = await CreateRepository(admin);
        var url = new UriBuilder(new Uri(admin.BaseAddress!, $"{name}.git")) { UserName = "admin", Password = "admin" }.Uri.ToString();
        var work = Path.Combine(TestContext.TestRunDirectory!, "work-" + Guid.NewGuid().ToString("N"));
        RunGit(TestContext.TestRunDirectory!, "init", "-q", "--initial-branch=trunk", work);
        await File.WriteAllTextAsync(Path.Combine(work, "readme.txt"), "pushed over http");
        RunGit(work, "add", "readme.txt");
        RunGit(work, "-c", "user.name=Test", "-c", "user.email=test@example.com", "commit", "-q", "-m", "first");

        RunGit(work, "-c", "credential.helper=", "push", "-q", url, "trunk");

        StringAssert.Contains(RunGit(work, "-c", "credential.helper=", "ls-remote", "--symref", url, "HEAD"), "ref: refs/heads/trunk\tHEAD");
        StringAssert.Contains(await admin.GetStringAsync($"/Repositories/{name}/Tree"), "readme.txt");
        var clone = Path.Combine(TestContext.TestRunDirectory!, "clone-" + Guid.NewGuid().ToString("N"));
        RunGit(TestContext.TestRunDirectory!, "-c", "credential.helper=", "clone", "-q", url, clone);
        Assert.IsTrue(File.Exists(Path.Combine(clone, "readme.txt")));
    }
}
