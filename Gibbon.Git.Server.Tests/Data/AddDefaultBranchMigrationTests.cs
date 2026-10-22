using System.Threading.Tasks;

using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Data;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Caching.Memory;

namespace Gibbon.Git.Server.Tests.Data;

[TestClass]
[TestCategory(TestCategories.UnitTest)]
public class AddDefaultBranchMigrationTests
{
    private const string PreviousMigration = "MergeRepositoryAdministratorsIntoAccessLevel";

    private SqliteConnection _connection = null!;
    private SqliteGibbonContext _context = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _context = new SqliteGibbonContext(new DbContextOptionsBuilder<SqliteGibbonContext>().UseSqlite(_connection).Options);
        await _context.GetService<IMigrator>().MigrateAsync(PreviousMigration);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [TestMethod]
    public async Task NewInstallation_GetsSeedBranch()
    {
        await _context.Database.MigrateAsync();

        var service = new ServerSettingsService(new MemoryCache(new MemoryCacheOptions()), _context);
        Assert.AreEqual(service.GetDefaultSettings().DefaultBranch, service.GetSettings().DefaultBranch);
    }

    [TestMethod]
    public async Task ExistingInstallationWithSettings_KeepsMaster()
    {
        await Execute("INSERT INTO ServerSettings (AllowAnonymousPush, AllowUserRepositoryCreation, AllowAnonymousRegistration, DefaultLanguage, IsCommitAuthorAvatarVisible, DisplayNameFormat, SortNameFormat, DateFormat, TimeFormat, DefaultRepositoryView, SiteTitle) VALUES (0, 1, 0, 'de-DE', 1, 0, 0, 0, 0, 0, 'Old')");

        await _context.Database.MigrateAsync();

        var settings = new ServerSettingsService(new MemoryCache(new MemoryCacheOptions()), _context).GetSettings();
        Assert.AreEqual("master", settings.DefaultBranch);
        Assert.AreEqual("Old", settings.SiteTitle);
    }

    [TestMethod]
    public async Task ExistingInstallationWithoutSettings_GetsSettingsWithMaster()
    {
        await Execute("INSERT INTO Repository (Name, Anonymous, AllowAnonymousPush, LinksUseGlobal) VALUES ('old', 0, 0, 1)");

        await _context.Database.MigrateAsync();

        var service = new ServerSettingsService(new MemoryCache(new MemoryCacheOptions()), _context);
        var settings = service.GetSettings();
        var defaults = service.GetDefaultSettings();
        Assert.AreEqual("master", settings.DefaultBranch);
        Assert.AreEqual(defaults.DefaultLanguage, settings.DefaultLanguage);
        Assert.AreEqual(defaults.AllowUserRepositoryCreation, settings.AllowUserRepositoryCreation);
    }

    private async Task Execute(string sql)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
