using System;
using System.Threading.Tasks;

using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Tests.TestHelper;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Gibbon.Git.Server.Tests.UserTests;

[TestClass]
public class EfSettingsNameFormatTests : DbTestBase<SqliteConnectionFactory>
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
    public async Task UserSettings_NameFormats_AreStored()
    {
        await _userSettingsService.SaveSettings(1, new UserSettings { DisplayNameFormat = NameFormat.FirstLast, SortNameFormat = NameFormat.LastFirst });
        ClearCache();

        var settings = await _userSettingsService.GetSettings(1);

        Assert.AreEqual(NameFormat.FirstLast, settings.DisplayNameFormat);
        Assert.AreEqual(NameFormat.LastFirst, settings.SortNameFormat);
    }

    [TestMethod]
    public async Task UserSettings_WithoutNameFormats_FallBackToServer()
    {
        await _userSettingsService.SaveSettings(1, new UserSettings { PreferredLanguage = "de" });
        ClearCache();

        var settings = await _userSettingsService.GetSettings(1);

        Assert.IsNull(settings.DisplayNameFormat);
        Assert.IsNull(settings.SortNameFormat);
    }

    [TestMethod]
    public async Task ServerSettings_NameFormats_AreStored()
    {
        await _serverSettingsService.SaveSettings(new ServerSettings { DefaultLanguage = "en", DisplayNameFormat = NameFormat.LastFirst, SortNameFormat = NameFormat.FirstLast });
        ClearCache();

        var settings = _serverSettingsService.GetSettings();

        Assert.AreEqual(NameFormat.LastFirst, settings.DisplayNameFormat);
        Assert.AreEqual(NameFormat.FirstLast, settings.SortNameFormat);
    }

    [TestMethod]
    public void ServerSettings_Default_IsLastCommaFirst()
    {
        var settings = _serverSettingsService.GetDefaultSettings();

        Assert.AreEqual(NameFormat.LastCommaFirst, settings.DisplayNameFormat);
        Assert.AreEqual(NameFormat.LastCommaFirst, settings.SortNameFormat);
    }

    private void ClearCache() => ((MemoryCache)ServiceProvider.GetRequiredService<IMemoryCache>()).Clear();
}
