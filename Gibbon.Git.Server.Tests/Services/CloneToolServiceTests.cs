using System.Linq;
using System.Security.Claims;

using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Services;

using Microsoft.AspNetCore.Http;

using NSubstitute;

namespace Gibbon.Git.Server.Tests.Services;

[TestClass]
[TestCategory(TestCategories.UnitTest)]
public class CloneToolServiceTests
{
    private readonly ServerSettings _serverSettings = new();
    private readonly UserSettings _userSettings = new();

    [TestMethod]
    public void GetAvailableTools_NotConfigured_ReturnsDefaultTools()
    {
        var tools = CreateService().GetAvailableTools();

        CollectionAssert.AreEqual(CloneTools.DefaultEnabledKeys.ToList(), tools.Select(t => t.Key).ToList());
    }

    [TestMethod]
    public void GetAvailableTools_EnabledAndCustom_KeepsCatalogOrderAndAppendsCustom()
    {
        _serverSettings.EnabledCloneTools = "tower,vscode";
        _serverSettings.CustomCloneTools = "Kiro|kiro://vscode.git/clone?url={url}\ninvalid line";

        var tools = CreateService().GetAvailableTools();

        CollectionAssert.AreEqual(new[] { "vscode", "tower", "custom:Kiro" }, tools.Select(t => t.Key).ToArray());
    }

    [TestMethod]
    public void GetPreferredTool_AvailableKey_ReturnsTool()
    {
        _userSettings.PreferredCloneTool = "rider";

        Assert.AreEqual("Rider", CreateService().GetPreferredTool()!.Name);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("tower")]
    [DataRow("custom:Gone")]
    public void GetPreferredTool_NotSetOrNotAvailable_ReturnsNull(string? key)
    {
        _userSettings.PreferredCloneTool = key;

        Assert.IsNull(CreateService().GetPreferredTool());
    }

    private CloneToolService CreateService()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "1")], "Test"))
        });
        var userSettingsService = Substitute.For<IUserSettingsService>();
        userSettingsService.GetSettings(1).Returns(_userSettings);

        return new CloneToolService(_serverSettings, new CurrentUserSettings(accessor, userSettingsService));
    }
}
