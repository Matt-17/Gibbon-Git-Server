using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Models;
using Gibbon.Git.Server.Services;

using Microsoft.AspNetCore.Http;

using NSubstitute;

namespace Gibbon.Git.Server.Tests.Services;

[TestClass]
[TestCategory(TestCategories.UnitTest)]
public class UserNameFormatterTests
{
    private static readonly UserModel John = new() { Username = "jsmith", GivenName = "John", Surname = "Smith" };
    private static readonly UserModel Anna = new() { Username = "azimmer", GivenName = "Anna", Surname = "Zimmer" };
    private static readonly UserModel Bob = new() { Username = "bmiller", GivenName = "Bob", Surname = "Miller" };
    private static readonly UserModel NoName = new() { Username = "build" };

    private IHttpContextAccessor _httpContextAccessor = null!;
    private IUserSettingsService _userSettingsService = null!;
    private ServerSettings _serverSettings = null!;

    [TestInitialize]
    public void Setup()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _userSettingsService = Substitute.For<IUserSettingsService>();
        _userSettingsService.GetDefaultSettings().Returns(new UserSettings());
        _serverSettings = new ServerSettings();
    }

    [TestMethod]
    [DataRow(NameFormat.LastCommaFirst, "Smith, John")]
    [DataRow(NameFormat.FirstLast, "John Smith")]
    [DataRow(NameFormat.LastFirst, "Smith John")]
    public void Format_FullName(NameFormat format, string expected)
    {
        Assert.AreEqual(expected, UserNameFormatter.Format(John, format));
    }

    [TestMethod]
    [DataRow(NameFormat.LastCommaFirst)]
    [DataRow(NameFormat.FirstLast)]
    [DataRow(NameFormat.LastFirst)]
    public void Format_PartialOrMissingName(NameFormat format)
    {
        Assert.AreEqual("John", UserNameFormatter.Format(new UserModel { Username = "j", GivenName = "John", Surname = "" }, format));
        Assert.AreEqual("Smith", UserNameFormatter.Format(new UserModel { Username = "s", GivenName = null, Surname = "Smith" }, format));
        Assert.AreEqual("build", UserNameFormatter.Format(NoName, format));
    }

    [TestMethod]
    public void Format_DefaultMatchesUserModelDisplayName()
    {
        Assert.AreEqual(John.DisplayName, UserNameFormatter.Format(John, default));
    }

    [TestMethod]
    public void GetDisplayName_Anonymous_UsesServerSetting()
    {
        _serverSettings.DisplayNameFormat = NameFormat.FirstLast;

        Assert.AreEqual("John Smith", CreateFormatter().GetDisplayName(John));
    }

    [TestMethod]
    public void GetDisplayName_UserSetting_OverridesServerSetting()
    {
        _serverSettings.DisplayNameFormat = NameFormat.FirstLast;
        LogIn(7, new UserSettings { DisplayNameFormat = NameFormat.LastFirst });

        Assert.AreEqual("Smith John", CreateFormatter().GetDisplayName(John));
    }

    [TestMethod]
    public void GetDisplayName_UserWithoutSetting_UsesServerSetting()
    {
        _serverSettings.DisplayNameFormat = NameFormat.FirstLast;
        LogIn(7, new UserSettings());

        Assert.AreEqual("John Smith", CreateFormatter().GetDisplayName(John));
    }

    [TestMethod]
    public void Sort_ByLastName()
    {
        _serverSettings.SortNameFormat = NameFormat.LastCommaFirst;

        var sorted = CreateFormatter().Sort([Anna, NoName, John, Bob]).Select(u => u.Username).ToArray();

        CollectionAssert.AreEqual(new[] { "build", "bmiller", "jsmith", "azimmer" }, sorted);
    }

    [TestMethod]
    public void Sort_ByFirstName()
    {
        LogIn(7, new UserSettings { SortNameFormat = NameFormat.FirstLast });

        var sorted = CreateFormatter().Sort([John, NoName, Bob, Anna]).Select(u => u.Username).ToArray();

        CollectionAssert.AreEqual(new[] { "azimmer", "bmiller", "build", "jsmith" }, sorted);
    }

    [TestMethod]
    public void Sort_ShorterSurnameFirst()
    {
        var mill = new UserModel { Username = "mill", GivenName = "Zoe", Surname = "Mill" };
        var miller = new UserModel { Username = "miller", GivenName = "Adam", Surname = "Miller" };

        var sorted = CreateFormatter().Sort([miller, mill]).Select(u => u.Username).ToArray();

        CollectionAssert.AreEqual(new[] { "mill", "miller" }, sorted);
    }

    [TestMethod]
    public void UserSettings_AreLoadedOncePerRequest()
    {
        LogIn(7, new UserSettings());
        var formatter = CreateFormatter();

        formatter.GetDisplayName(John);
        formatter.GetDisplayName(Anna);
        formatter.Sort([John, Anna]).ToList();

        _userSettingsService.Received(1).GetSettings(7);
    }

    private void LogIn(int userId, UserSettings settings)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Test");
        _httpContextAccessor.HttpContext.Returns(new DefaultHttpContext { User = new ClaimsPrincipal(identity) });
        _userSettingsService.GetSettings(userId).Returns(Task.FromResult(settings));
    }

    private UserNameFormatter CreateFormatter() => new(_serverSettings, new CurrentUserSettings(_httpContextAccessor, _userSettingsService));
}
