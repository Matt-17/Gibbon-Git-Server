using System.Linq;
using System.Net.Http.Headers;
using System.Text;

using Gibbon.Git.Server.Data;
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

    [TestMethod]
    public async Task Manager_CanEditButCannotRemoveOwnManageAccess()
    {
        var admin = await CreateClientAsync(true);
        var (username, userId) = await CreateMember(admin);
        var name = await CreateRepository(admin, ("PostedSelectedUsers", userId.ToString()), ($"UserAccessLevels[{userId}]", "Administer"));
        var manager = await CreateMemberClient(username);

        Assert.AreEqual(HttpStatusCode.OK, (await manager.GetAsync($"/Repositories/{name}/Edit")).StatusCode);

        var postData = new Dictionary<string, string>(await GetAntiForgeryToken(manager, $"/Repositories/{name}/Edit"))
        {
            { "Id", GetRepositoryId(name).ToString() },
            { "Name", name },
            { "PostedSelectedUsers", userId.ToString() },
            { $"UserAccessLevels[{userId}]", "Push" }
        };
        var response = await manager.PostAsync($"/Repositories/{name}/Edit", new FormUrlEncodedContent(postData));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "field-validation-error");
        Assert.AreEqual(HttpStatusCode.OK, await GetInfoRefs(name, "git-receive-pack", username));
        Assert.AreEqual(HttpStatusCode.OK, (await manager.GetAsync($"/Repositories/{name}/Edit")).StatusCode);
    }

    [TestMethod]
    public async Task WritingMember_CannotEdit()
    {
        var admin = await CreateClientAsync(true);
        var (username, userId) = await CreateMember(admin);
        var name = await CreateRepository(admin, ("PostedSelectedUsers", userId.ToString()));
        var member = await CreateMemberClient(username);

        Assert.AreEqual(HttpStatusCode.Forbidden, (await member.GetAsync($"/Repositories/{name}/Edit")).StatusCode);
    }

    [TestMethod]
    public async Task ManagingTeamMember_CanEdit()
    {
        var admin = await CreateClientAsync(true);
        var (username, userId) = await CreateMember(admin);
        var teamId = await CreateTeam(admin, userId);
        var name = await CreateRepository(admin, ("PostedSelectedTeams", teamId.ToString()), ($"TeamAccessLevels[{teamId}]", "Administer"));
        var member = await CreateMemberClient(username);

        Assert.AreEqual(HttpStatusCode.OK, (await member.GetAsync($"/Repositories/{name}/Edit")).StatusCode);
    }

    private async Task<HttpClient> CreateMemberClient(string username)
    {
        var client = Application.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await AuthenticateUserAsync(client, username, MemberPassword);

        return client;
    }

    private int GetRepositoryId(string name)
    {
        using var scope = Application.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IRepositoryService>().GetRepository(name).Id;
    }

    private async Task<int> CreateTeam(HttpClient admin, int memberId)
    {
        var teamName = "team" + Guid.NewGuid().ToString("N")[..6];
        var postData = new Dictionary<string, string>(await GetAntiForgeryToken(admin, "/Teams/Create"))
        {
            { "Name", teamName },
            { "PostedSelectedUsers", memberId.ToString() }
        };
        var response = await admin.PostAsync("/Teams/Create", new FormUrlEncodedContent(postData));
        Assert.IsTrue(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect, response.StatusCode.ToString());

        using var scope = Application.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ITeamService>().GetAllTeams().Single(t => t.Name == teamName).Id;
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
