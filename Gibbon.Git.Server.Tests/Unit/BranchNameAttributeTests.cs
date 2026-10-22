using Gibbon.Git.Server.Middleware.Attributes;

namespace Gibbon.Git.Server.Tests.Unit;

[TestClass]
[TestCategory(TestCategories.UnitTest)]
public class BranchNameAttributeTests
{
    [TestMethod]
    [DataRow("trunk")]
    [DataRow("release/1.0")]
    [DataRow("feature/x-y_z")]
    [DataRow("ümlaut")]
    public void ValidBranchName_IsAccepted(string name)
    {
        Assert.IsTrue(BranchNameAttribute.IsValidBranchName(name));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("bad name")]
    [DataRow("HEAD")]
    [DataRow("@")]
    [DataRow("-start")]
    [DataRow("/start")]
    [DataRow("end/")]
    [DataRow("end.")]
    [DataRow("a..b")]
    [DataRow("a//b")]
    [DataRow("a@{b")]
    [DataRow("a~b")]
    [DataRow("a^b")]
    [DataRow("a:b")]
    [DataRow("a?b")]
    [DataRow("a*b")]
    [DataRow("a[b")]
    [DataRow("a\\b")]
    [DataRow(".hidden")]
    [DataRow("a/.hidden")]
    [DataRow("name.lock")]
    [DataRow("a/name.lock/b")]
    public void InvalidBranchName_IsRejected(string name)
    {
        Assert.IsFalse(BranchNameAttribute.IsValidBranchName(name));
    }
}
