using System;
using System.Threading.Tasks;

using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Tests.TestHelper;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Gibbon.Git.Server.Tests.UserTests;

[TestClass]
public class EfSettingsDateFormatTests : DbTestBase<SqliteConnectionFactory>
{
    private IUserSettingsService _userSettingsService = null!;
    private IServerSettingsService _serverSettingsService = null!;

    protected override void ConfigureServices(ServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<IUserSettingsService, UserSettingsService>();
        services.AddScoped<IServerSettingsService, ServerSettingsService>();
    }

    protected override void UseServices(IServiceProvider serviceProvider)
    {
        _userSettingsService = serviceProvider.GetRequiredService<IUserSettingsService>();
        _serverSettingsService = serviceProvider.GetRequiredService<IServerSettingsService>();
    }

    [TestMethod]
    public async Task UserSettings_DateAndTimeFormats_AreStored()
    {
        await _userSettingsService.SaveSettings(1, new UserSettings { DateFormat = DateFormat.DayMonthNameYear, TimeFormat = TimeFormat.Hidden });
        ClearCache();

        var settings = await _userSettingsService.GetSettings(1);

        Assert.AreEqual(DateFormat.DayMonthNameYear, settings.DateFormat);
        Assert.AreEqual(TimeFormat.Hidden, settings.TimeFormat);
    }

    [TestMethod]
    public async Task ServerSettings_DateAndTimeFormats_AreStored()
    {
        await _serverSettingsService.SaveSettings(new ServerSettings { DefaultLanguage = "en", DateFormat = DateFormat.YearMonthDay, TimeFormat = TimeFormat.Hours24 });
        ClearCache();

        var settings = _serverSettingsService.GetSettings();

        Assert.AreEqual(DateFormat.YearMonthDay, settings.DateFormat);
        Assert.AreEqual(TimeFormat.Hours24, settings.TimeFormat);
    }

    [TestMethod]
    public async Task UserSettings_DefaultRepositoryView_IsStored()
    {
        await _userSettingsService.SaveSettings(1, new UserSettings { DefaultRepositoryView = RepositoryView.Commits });
        ClearCache();

        Assert.AreEqual(RepositoryView.Commits, (await _userSettingsService.GetSettings(1)).DefaultRepositoryView);
    }

    [TestMethod]
    public async Task ServerSettings_DefaultRepositoryView_IsStored()
    {
        await _serverSettingsService.SaveSettings(new ServerSettings { DefaultLanguage = "en", DefaultRepositoryView = RepositoryView.Tree });
        ClearCache();

        Assert.AreEqual(RepositoryView.Tree, _serverSettingsService.GetSettings().DefaultRepositoryView);
    }

    [TestMethod]
    public async Task ServerSettings_DefaultBranch_IsStored()
    {
        await _serverSettingsService.SaveSettings(new ServerSettings { DefaultLanguage = "en", DefaultBranch = "trunk" });
        ClearCache();

        Assert.AreEqual("trunk", _serverSettingsService.GetSettings().DefaultBranch);
    }

    [TestMethod]
    public async Task ServerSettings_EmptyDefaultBranch_KeepsCurrentOne()
    {
        await _serverSettingsService.SaveSettings(new ServerSettings { DefaultLanguage = "en", DefaultBranch = "trunk" });
        var settings = new ServerSettings { DefaultLanguage = "en" };

        await _serverSettingsService.SaveSettings(settings);
        ClearCache();

        Assert.AreEqual("trunk", settings.DefaultBranch);
        Assert.AreEqual("trunk", _serverSettingsService.GetSettings().DefaultBranch);
    }

    [TestMethod]
    public async Task ServerSettings_FirstSaveWithoutDefaultBranch_UsesDefault()
    {
        await _serverSettingsService.SaveSettings(new ServerSettings { DefaultLanguage = "en" });
        ClearCache();

        Assert.AreEqual(_serverSettingsService.GetDefaultSettings().DefaultBranch, _serverSettingsService.GetSettings().DefaultBranch);
    }

    private void ClearCache() => ((MemoryCache)ServiceProvider.GetRequiredService<IMemoryCache>()).Clear();
}
