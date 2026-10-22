using System;
using System.IO;

using Gibbon.Git.Server.Configuration;

using Microsoft.Extensions.Configuration;

namespace Gibbon.Git.Server.Tests.Unit;

[TestClass]
[TestCategory(TestCategories.UnitTest)]
public class AppSettingsExampleTests
{
    private static IConfiguration LoadExample()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.example.json");
        Assert.IsTrue(File.Exists(path), path);

        return new ConfigurationBuilder()
            .AddJsonFile(path, optional: false, reloadOnChange: false)
            .Build();
    }

    [TestMethod]
    public void Example_UsesSqlite()
    {
        var database = LoadExample().GetSection("Database");

        Assert.AreEqual(DatabaseProviderTypes.Sqlite, database.GetValue<DatabaseProviderTypes>("Provider"));
        Assert.AreEqual("Data Source=App_Data/Gibbon.Git.Server.db", database.GetConnectionString("Sqlite"));
        Assert.AreEqual(@"Server=.\SQLEXPRESS;Database=Gibbon;Integrated Security=true;TrustServerCertificate=true", database.GetConnectionString("SqlServer"));
    }

    [TestMethod]
    public void Example_BindsAllSections()
    {
        var config = LoadExample();

        Assert.IsTrue(config.GetSection("Database").Get<DatabaseSettings>()!.AllowMigration);
        Assert.AreEqual("App_Data", config.GetSection("Application").Get<ApplicationSettings>()!.DataPath);
        Assert.AreEqual("git", config.GetSection("Git").Get<GitSettings>()!.Executable);
        Assert.AreEqual(587, config.GetSection("Mail").Get<MailSettings>()!.Port);
    }
}
