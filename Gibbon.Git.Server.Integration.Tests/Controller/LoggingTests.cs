using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gibbon.Git.Server.Integration.Tests.Controller;

[TestClass]
internal class LoggingTests : IntegrationTestBase
{
    [TestMethod]
    public void Warning_IsWrittenToLogFileInDataPath()
    {
        var marker = Guid.NewGuid().ToString("N");
        var logger = Application.Services.GetRequiredService<ILogger<LoggingTests>>();

        logger.LogWarning("Log file test {Marker}", marker);

        Assert.IsTrue(ReadLogFiles().Any(c => c.Contains($"[WRN] {typeof(LoggingTests).FullName}: Log file test {marker}")));
    }

    [TestMethod]
    public void Debug_IsFilteredByLogLevelConfiguration()
    {
        var marker = Guid.NewGuid().ToString("N");
        var logger = Application.Services.GetRequiredService<ILogger<LoggingTests>>();

        logger.LogDebug("Filtered {Marker}", marker);
        logger.LogWarning("Written {Marker}", marker);

        var content = ReadLogFiles().ToList();
        Assert.IsTrue(content.Any(c => c.Contains($"Written {marker}")));
        Assert.IsFalse(content.Any(c => c.Contains($"Filtered {marker}")));
    }

    private IEnumerable<string> ReadLogFiles()
    {
        var logDirectory = Path.Combine(TestContext.TestRunDirectory!, "Databases", "Logs");
        return Directory.GetFiles(logDirectory, "gibbon-*.log").Select(ReadShared);
    }

    private static string ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
