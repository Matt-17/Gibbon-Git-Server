using System.IO;

using Gibbon.Git.Server.Configuration;

using Microsoft.Data.Sqlite;

namespace Gibbon.Git.Server.Tests.Unit;

[TestClass]
public class DataPathDefaultsTests
{
    private static readonly string RootPath = Path.Combine(Path.GetTempPath(), "gibbon-root");
    private static readonly string DataPath = Path.Combine(Path.GetTempPath(), "gibbon-data");

    [TestMethod]
    public void Apply_NoPathsConfigured_UsesAppDataDefaults()
    {
        var settings = new ApplicationSettings();

        DataPathDefaults.Apply(settings);

        Assert.AreEqual("App_Data", settings.DataPath);
        Assert.AreEqual(Path.Combine("App_Data", "Repositories"), settings.RepositoryPath);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void Apply_EmptyDataPath_FallsBackToAppData(string? dataPath)
    {
        var settings = new ApplicationSettings { DataPath = dataPath! };

        DataPathDefaults.Apply(settings);

        Assert.AreEqual("App_Data", settings.DataPath);
    }

    [TestMethod]
    public void Apply_OnlyDataPathConfigured_RepositoriesFollowDataPath()
    {
        var settings = new ApplicationSettings { DataPath = DataPath };

        DataPathDefaults.Apply(settings);

        Assert.AreEqual(Path.Combine(DataPath, "Repositories"), settings.RepositoryPath);
    }

    [TestMethod]
    public void Apply_OnlyDataPathConfigured_LogsFollowDataPath()
    {
        var settings = new ApplicationSettings { DataPath = DataPath };

        DataPathDefaults.Apply(settings);

        Assert.AreEqual(Path.Combine(DataPath, "Logs"), settings.LogPath);
        Assert.IsTrue(settings.LogToFile);
    }

    [TestMethod]
    public void Apply_ExplicitLogPath_IsKept()
    {
        var settings = new ApplicationSettings { DataPath = DataPath, LogPath = "custom-logs" };

        DataPathDefaults.Apply(settings);

        Assert.AreEqual("custom-logs", settings.LogPath);
    }

    [TestMethod]
    public void Apply_ExplicitRepositoryPath_IsKept()
    {
        var settings = new ApplicationSettings { DataPath = DataPath, RepositoryPath = "App_Data\\Repositories" };

        DataPathDefaults.Apply(settings);

        Assert.AreEqual("App_Data\\Repositories", settings.RepositoryPath);
    }

    [TestMethod]
    public void ApplyGit_NoPathsConfigured_FollowDataPath()
    {
        var git = new GitSettings();

        DataPathDefaults.Apply(git, new ApplicationSettings { DataPath = DataPath });

        Assert.AreEqual(DataPath, git.HomePath);
        Assert.AreEqual(Path.Combine(DataPath, "Git"), git.BinaryPath);
    }

    [TestMethod]
    public void ApplyGit_ExplicitPaths_AreKept()
    {
        var git = new GitSettings { HomePath = "App_Data", BinaryPath = "App_Data\\Git" };

        DataPathDefaults.Apply(git, new ApplicationSettings { DataPath = DataPath });

        Assert.AreEqual("App_Data", git.HomePath);
        Assert.AreEqual("App_Data\\Git", git.BinaryPath);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void ResolveSqliteConnectionString_NoDataSource_UsesDatabaseInDataPath(string? connectionString)
    {
        var resolved = DataPathDefaults.ResolveSqliteConnectionString(connectionString!, RootPath, DataPath);

        Assert.AreEqual(Path.Combine(DataPath, "Gibbon.Git.Server.db"), DataSource(resolved));
    }

    [TestMethod]
    [DataRow("Data Source=App_Data\\Gibbon.Git.Server.db;")]
    [DataRow("Data Source=App_Data/Gibbon.Git.Server.db")]
    public void ResolveSqliteConnectionString_RelativeDataSource_IsResolvedAgainstContentRoot(string connectionString)
    {
        var resolved = DataPathDefaults.ResolveSqliteConnectionString(connectionString, RootPath, DataPath);

        Assert.AreEqual(Path.Combine(RootPath, "App_Data", "Gibbon.Git.Server.db"), DataSource(resolved));
    }

    [TestMethod]
    public void ResolveSqliteConnectionString_AbsoluteDataSource_IsUnchanged()
    {
        var database = Path.Combine(DataPath, "custom.db");

        var resolved = DataPathDefaults.ResolveSqliteConnectionString($"Data Source={database};Cache=Shared", RootPath, DataPath);

        Assert.AreEqual(database, DataSource(resolved));
        Assert.AreEqual(SqliteCacheMode.Shared, new SqliteConnectionStringBuilder(resolved).Cache);
    }

    [TestMethod]
    [DataRow("Data Source=:memory:", ":memory:")]
    [DataRow("Data Source=|DataDirectory|gibbon.db", "|DataDirectory|gibbon.db")]
    public void ResolveSqliteConnectionString_SpecialDataSource_IsUnchanged(string connectionString, string expected)
    {
        var resolved = DataPathDefaults.ResolveSqliteConnectionString(connectionString, RootPath, DataPath);

        Assert.AreEqual(expected, DataSource(resolved));
    }

    private static string DataSource(string connectionString) => new SqliteConnectionStringBuilder(connectionString).DataSource;
}
