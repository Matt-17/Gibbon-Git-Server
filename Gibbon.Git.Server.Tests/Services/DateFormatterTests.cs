using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Services;

using Microsoft.AspNetCore.Http;

using NSubstitute;

namespace Gibbon.Git.Server.Tests.Services;

[TestClass]
[TestCategory(TestCategories.UnitTest)]
public class DateFormatterTests
{
    private static readonly DateTime Value = new(2026, 3, 7, 17, 5, 9);

    private CultureInfo _culture = null!;
    private IHttpContextAccessor _httpContextAccessor = null!;
    private IUserSettingsService _userSettingsService = null!;
    private ServerSettings _serverSettings = null!;

    [TestInitialize]
    public void Setup()
    {
        _culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _userSettingsService = Substitute.For<IUserSettingsService>();
        _userSettingsService.GetDefaultSettings().Returns(new UserSettings());
        _serverSettings = new ServerSettings();
    }

    [TestCleanup]
    public void Cleanup()
    {
        CultureInfo.CurrentCulture = _culture;
    }

    [TestMethod]
    [DataRow(DateFormat.YearMonthDay, "2026-03-07")]
    [DataRow(DateFormat.DayMonthYearDots, "07.03.2026")]
    [DataRow(DateFormat.DayMonthYearSlashes, "07/03/2026")]
    [DataRow(DateFormat.MonthDayYearSlashes, "03/07/2026")]
    [DataRow(DateFormat.DayMonthNameYear, "7 März 2026")]
    [DataRow(DateFormat.MonthNameDayYear, "März 7, 2026")]
    [DataRow(DateFormat.Language, "07.03.2026")]
    public void FormatDate(DateFormat format, string expected)
    {
        Assert.AreEqual(expected, DateFormatter.Format(Value, format));
    }

    [TestMethod]
    [DataRow(TimeFormat.Hours24, "17:05")]
    [DataRow(TimeFormat.Hours24WithSeconds, "17:05:09")]
    [DataRow(TimeFormat.Hours12, "5:05 PM")]
    [DataRow(TimeFormat.Language, "17:05:09")]
    [DataRow(TimeFormat.Hidden, "")]
    public void FormatTime(TimeFormat format, string expected)
    {
        Assert.AreEqual(expected, DateFormatter.Format(Value, format));
    }

    [TestMethod]
    public void NumericFormats_DoNotDependOnCultureSeparators()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fi-FI");

        Assert.AreEqual("03/07/2026", DateFormatter.Format(Value, DateFormat.MonthDayYearSlashes));
        Assert.AreEqual("17:05", DateFormatter.Format(Value, TimeFormat.Hours24));
    }

    [TestMethod]
    public void Defaults_MatchPreviousOutput()
    {
        Assert.AreEqual(Value.ToString(CultureInfo.CurrentCulture), CreateFormatter().FormatDateTime(Value));
    }

    [TestMethod]
    public void FormatDateTime_HiddenTime_ShowsDateOnly()
    {
        _serverSettings.DateFormat = DateFormat.YearMonthDay;
        _serverSettings.TimeFormat = TimeFormat.Hidden;

        Assert.AreEqual("2026-03-07", CreateFormatter().FormatDateTime(Value));
    }

    [TestMethod]
    public void FormatDateTime_Null_IsEmpty()
    {
        Assert.AreEqual(string.Empty, CreateFormatter().FormatDateTime(null));
    }

    [TestMethod]
    public void UserSettings_OverrideServerSettings()
    {
        _serverSettings.DateFormat = DateFormat.YearMonthDay;
        _serverSettings.TimeFormat = TimeFormat.Hours24;
        LogIn(new UserSettings { DateFormat = DateFormat.MonthDayYearSlashes, TimeFormat = TimeFormat.Hours12 });

        Assert.AreEqual("03/07/2026 5:05 PM", CreateFormatter().FormatDateTime(Value));
    }

    [TestMethod]
    public void UserWithoutSettings_UsesServerSettings()
    {
        _serverSettings.DateFormat = DateFormat.DayMonthYearDots;
        _serverSettings.TimeFormat = TimeFormat.Hours24;
        LogIn(new UserSettings());

        var formatter = CreateFormatter();

        Assert.AreEqual("07.03.2026 17:05", formatter.FormatDateTime(Value));
        Assert.AreEqual("07.03.2026", formatter.FormatDate(Value));
    }

    [TestMethod]
    public void Options_ContainEveryFormatWithExample()
    {
        var formatter = CreateFormatter();

        var dateOptions = formatter.GetDateFormatOptions();
        var timeOptions = formatter.GetTimeFormatOptions();

        Assert.HasCount(Enum.GetValues<DateFormat>().Length, dateOptions);
        Assert.HasCount(Enum.GetValues<TimeFormat>().Length, timeOptions);
        Assert.IsTrue(dateOptions.Single(o => o.Value == "1").Text.EndsWith("-12-24"));
        Assert.IsTrue(timeOptions.Single(o => o.Value == "1").Text == "17:30");
    }

    private void LogIn(UserSettings settings)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "7")], "Test");
        _httpContextAccessor.HttpContext.Returns(new DefaultHttpContext { User = new ClaimsPrincipal(identity) });
        _userSettingsService.GetSettings(7).Returns(Task.FromResult(settings));
    }

    private DateFormatter CreateFormatter() => new(_serverSettings, new CurrentUserSettings(_httpContextAccessor, _userSettingsService));
}
