namespace Gibbon.Git.Server.Integration.Tests.Controller;

[TestClass]
internal class DefaultConfigurationTests : IntegrationTestBase
{
    protected override string? DatabaseProvider => null;

    [TestMethod]
    public async Task WithoutDatabaseConfiguration_UsesSqliteInDataPath()
    {
        var database = Path.Combine(TestContext.TestRunDirectory!, "Databases", "Gibbon.Git.Server.db");
        File.Delete(database);

        var client = await CreateClientAsync(true);

        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/Repositories")).StatusCode);
        Assert.IsTrue(File.Exists(database));
    }
}
