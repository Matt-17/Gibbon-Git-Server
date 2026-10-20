using System;
using System.IO;

using Gibbon.Git.Server.Services;

namespace Gibbon.Git.Server.Tests.Services;

[TestClass]
[TestCategory(TestCategories.UnitTest)]
public class DiagnosticReporterTests
{
    private string _directory = null!;

    [TestInitialize]
    public void Setup()
    {
        _directory = Path.Combine(Path.GetTempPath(), "gibbon-logs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Directory.Delete(_directory, true);
    }

    [TestMethod]
    public void ReadLogTail_ReturnsEndOfNewestLogFile()
    {
        var older = Path.Combine(_directory, "gibbon-20261019.log");
        File.WriteAllText(older, "old");
        File.SetLastWriteTimeUtc(older, DateTime.UtcNow.AddDays(-1));
        File.WriteAllText(Path.Combine(_directory, "gibbon-20261020.log"), "0123456789");
        File.WriteAllText(Path.Combine(_directory, "other.log"), "ignored");

        var tail = DiagnosticReporter.ReadLogTail(_directory, 4);

        Assert.IsNotNull(tail);
        Assert.AreEqual("gibbon-20261020.log", tail.Value.FileName);
        Assert.AreEqual(10, tail.Value.Length);
        Assert.AreEqual("6789", tail.Value.Text);
    }

    [TestMethod]
    public void ReadLogTail_FileOpenForWriting_CanBeRead()
    {
        var path = Path.Combine(_directory, "gibbon-20261020.log");
        using var writer = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        writer.Write("line\n"u8);
        writer.Flush();

        var tail = DiagnosticReporter.ReadLogTail(_directory, 100);

        Assert.AreEqual("line\n", tail!.Value.Text);
    }

    [TestMethod]
    public void ReadLogTail_NoLogFile_ReturnsNull()
    {
        Assert.IsNull(DiagnosticReporter.ReadLogTail(_directory, 100));
        Assert.IsNull(DiagnosticReporter.ReadLogTail(Path.Combine(_directory, "missing"), 100));
    }
}
