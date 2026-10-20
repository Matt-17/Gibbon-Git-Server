using System;
using System.IO;
using System.Linq;

using Gibbon.Git.Server.Services;

namespace Gibbon.Git.Server.Tests.Services;

[TestClass]
[TestCategory(TestCategories.UnitTest)]
public class CultureServiceTests
{
    private string _directory = null!;

    [TestInitialize]
    public void Setup()
    {
        _directory = Path.Combine(Path.GetTempPath(), "gibbon-cultures-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Directory.Delete(_directory, true);
    }

    [TestMethod]
    public void GetSupportedCultures_UsesSatelliteAssemblies()
    {
        CreateSatellite("fr", "Gibbon.Git.Server.resources.dll");
        CreateSatellite("zh-CN", "Gibbon.Git.Server.resources.dll");
        CreateSatellite("de", "Microsoft.Data.SqlClient.resources.dll");
        Directory.CreateDirectory(Path.Combine(_directory, "wwwroot"));

        var cultures = CultureService.GetSupportedCultures(_directory).Select(c => c.Name).ToArray();

        CollectionAssert.AreEqual(new[] { "en", "fr", "zh-CN" }, cultures);
    }

    [TestMethod]
    public void GetSupportedCultures_WithoutSatellites_ReturnsEnglish()
    {
        var cultures = CultureService.GetSupportedCultures(_directory).Select(c => c.Name).ToArray();

        CollectionAssert.AreEqual(new[] { "en" }, cultures);
    }

    [TestMethod]
    public void GetSupportedCultures_BuildOutput_ContainsShippedLanguages()
    {
        var cultures = CultureService.GetSupportedCultures(AppContext.BaseDirectory).Select(c => c.Name).ToArray();

        CollectionAssert.IsSubsetOf(new[] { "de", "en", "fr", "zh-CN" }, cultures);
    }

    private void CreateSatellite(string culture, string fileName)
    {
        var directory = Directory.CreateDirectory(Path.Combine(_directory, culture));
        File.WriteAllBytes(Path.Combine(directory.FullName, fileName), []);
    }
}
