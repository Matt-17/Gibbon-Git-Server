using System;
using System.Threading.Tasks;

using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Data;

using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Caching.Memory;

namespace Gibbon.Git.Server.Tests.Data;

public abstract class AddDefaultBranchMigrationTests
{
    private const string PreviousMigration = "MergeRepositoryAdministratorsIntoAccessLevel";

    protected GibbonGitServerContext Context { get; set; } = null!;

    protected abstract GibbonGitServerContext CreateContext();

    [TestInitialize]
    public async Task Setup()
    {
        Context = CreateContext();
        await Context.GetService<IMigrator>().MigrateAsync(PreviousMigration);
    }

    [TestCleanup]
    public virtual void Cleanup()
    {
        Context.Dispose();
    }

    [TestMethod]
    public async Task NewInstallation_GetsSeedBranch()
    {
        await Context.Database.MigrateAsync();

        var service = CreateService();
        Assert.AreEqual(service.GetDefaultSettings().DefaultBranch, service.GetSettings().DefaultBranch);
    }

    [TestMethod]
    public async Task ExistingInstallationWithSettings_KeepsMaster()
    {
        await Context.Database.ExecuteSqlRawAsync("INSERT INTO ServerSettings (AllowAnonymousPush, AllowUserRepositoryCreation, AllowAnonymousRegistration, DefaultLanguage, IsCommitAuthorAvatarVisible, DisplayNameFormat, SortNameFormat, DateFormat, TimeFormat, DefaultRepositoryView, SiteTitle) VALUES (0, 1, 0, 'de-DE', 1, 0, 0, 0, 0, 0, 'Old')");

        await Context.Database.MigrateAsync();

        var settings = CreateService().GetSettings();
        Assert.AreEqual("master", settings.DefaultBranch);
        Assert.AreEqual("Old", settings.SiteTitle);
    }

    [TestMethod]
    public async Task ExistingInstallationWithoutSettings_GetsSettingsWithMaster()
    {
        await Context.Database.ExecuteSqlRawAsync("INSERT INTO Repository (Name, Anonymous, AllowAnonymousPush, LinksUseGlobal) VALUES ('old', 0, 0, 1)");

        await Context.Database.MigrateAsync();

        var service = CreateService();
        var settings = service.GetSettings();
        var defaults = service.GetDefaultSettings();
        Assert.AreEqual("master", settings.DefaultBranch);
        Assert.AreEqual(defaults.DefaultLanguage, settings.DefaultLanguage);
        Assert.AreEqual(defaults.AllowUserRepositoryCreation, settings.AllowUserRepositoryCreation);
    }

    private ServerSettingsService CreateService() => new(new MemoryCache(new MemoryCacheOptions()), Context);
}

[TestClass]
[TestCategory(TestCategories.UnitTest)]
public class SqliteAddDefaultBranchMigrationTests : AddDefaultBranchMigrationTests
{
    private SqliteConnection _connection = null!;

    protected override GibbonGitServerContext CreateContext()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        return new SqliteGibbonContext(new DbContextOptionsBuilder<SqliteGibbonContext>().UseSqlite(_connection).Options);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        _connection.Dispose();
    }
}

/// <summary>
/// Runs against a real SQL Server when GIBBON_TEST_SQLSERVER holds a connection string without a database,
/// e.g. <c>Server=(localdb)\MSSQLLocalDB;Integrated Security=true</c>.
/// </summary>
[TestClass]
public class SqlServerAddDefaultBranchMigrationTests : AddDefaultBranchMigrationTests
{
    protected override GibbonGitServerContext CreateContext()
    {
        var server = Environment.GetEnvironmentVariable("GIBBON_TEST_SQLSERVER");
        if (string.IsNullOrEmpty(server))
        {
            Assert.Inconclusive("GIBBON_TEST_SQLSERVER is not set.");
        }

        var connectionString = new SqlConnectionStringBuilder(server) { InitialCatalog = "GibbonMigrationTest_" + Guid.NewGuid().ToString("N") }.ConnectionString;
        return new SqlServerGibbonContext(new DbContextOptionsBuilder<SqlServerGibbonContext>().UseSqlServer(connectionString).Options);
    }

    public override void Cleanup()
    {
        Context?.Database.EnsureDeleted();
        base.Cleanup();
    }
}
