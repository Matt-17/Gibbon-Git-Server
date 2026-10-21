using System.Net.Http.Headers;
using System.Text;

using Gibbon.Git.Server.Security;

using Microsoft.Extensions.DependencyInjection;

using static Gibbon.Git.Server.Integration.Tests.Helper.RepositoryTestHelper;

namespace Gibbon.Git.Server.Integration.Tests.Controller;

[TestClass]
internal class RepositoriesMemberAccessTests : IntegrationTestBase
{
    private const string MemberPassword = "Member123!";

    [TestMethod]
    public async Task ReadOnlyMember_CanPullButNotPush()
    {
        var admin = await CreateClientAsync(true);
        var (username, userId) = await CreateMember(admin);
        var name = await CreateRepository(admin, ("PostedSelectedUsers", userId.ToString()), ($"UserAccessLevels[{userId}]", "Pull"));

        Assert.AreEqual(HttpStatusCode.OK, await GetInfoRefs(name, "git-upload-pack", username));
        Assert.AreEqual(HttpStatusCode.Forbidden, await GetInfoRefs(name, "git-receive-pack", username));
    }

    [TestMethod]
    public async Task MemberWithoutLevel_CanPush()
    {
        var admin = await CreateClientAsync(true);
        var (username, userId) = await CreateMember(admin);
        var name = await CreateRepository(admin, ("PostedSelectedUsers", userId.ToString()));

        Assert.AreEqual(HttpStatusCode.OK, await GetInfoRefs(name, "git-receive-pack", username));
    }

    [TestMethod]
    public async Task ReadOnlyMember_IsMarkedOnDetailPage()
    {
        var admin = await CreateClientAsync(true);
        var (_, userId) = await CreateMember(admin);
        var name = await CreateRepository(admin, ("PostedSelectedUsers", userId.ToString()), ($"UserAccessLevels[{userId}]", "Pull"));

        var detail = await admin.GetStringAsync($"/Repositories/{name}/Detail");
        var edit = await admin.GetStringAsync($"/Repositories/{name}/Edit");

        StringAssert.Contains(detail, "access-badge");
        StringAssert.Contains(edit, $"name=\"UserAccessLevels[{userId}]\"");
        StringAssert.Contains(edit, "<option value=\"Pull\" selected=\"selected\">");
    }

    private async Task<HttpStatusCode> GetInfoRefs(string name, string service, string username)
    {
        var git = Application.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var request = new HttpRequestMessage(HttpMethod.Get, $"/{name}.git/info/refs?service={service}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{MemberPassword}")));

        return (await git.SendAsync(request)).StatusCode;
    }

    private async Task<(string Username, int Id)> CreateMember(HttpClient admin)
    {
        var username = "member" + Guid.NewGuid().ToString("N")[..6];
        var postData = new Dictionary<string, string>(await GetAntiForgeryToken(admin, "/Users/Create"))
        {
            { "Username", username },
            { "Name", "Repo" },
            { "Surname", "Member" },
            { "Email", $"{username}@example.com" },
            { "Password", MemberPassword },
            { "ConfirmPassword", MemberPassword }
        };
        var response = await admin.PostAsync("/Users/Create", new FormUrlEncodedContent(postData));
        response.EnsureSuccessStatusCode();

        using var scope = Application.Services.CreateScope();
        var user = scope.ServiceProvider.GetRequiredService<IUserService>().GetUserModel(username);

        return (username, user.Id);
    }
}
