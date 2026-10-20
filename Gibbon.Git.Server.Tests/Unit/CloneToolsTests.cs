using System.Collections.Generic;
using System.Linq;

using Gibbon.Git.Server.Configuration;

namespace Gibbon.Git.Server.Tests.Unit;

[TestClass]
[TestCategory(TestCategories.UnitTest)]
public class CloneToolsTests
{
    private const string GitUrl = "https://mv@server/demo repo.git";

    [TestMethod]
    public void BuildUrl_EncodesGitUrl()
    {
        var tool = CloneTools.BuiltIn.Single(t => t.Key == "vscode");

        Assert.AreEqual("vscode://vscode.git/clone?url=https%3A%2F%2Fmv%40server%2Fdemo%20repo.git", tool.BuildUrl(GitUrl));
    }

    [TestMethod]
    [DataRow("idea", "idea")]
    [DataRow("rider", "rd")]
    [DataRow("webstorm", "web-storm")]
    [DataRow("phpstorm", "php-storm")]
    public void BuiltIn_JetBrains_UsesToolboxTags(string key, string tag)
    {
        var tool = CloneTools.BuiltIn.Single(t => t.Key == key);

        Assert.AreEqual($"jetbrains://{tag}/checkout/git?checkout.repo=https%3A%2F%2Fserver%2Fdemo.git&idea.required.plugins.id=Git4Idea", tool.BuildUrl("https://server/demo.git"));
    }

    [TestMethod]
    public void BuiltIn_KeysAreUnique_AndDefaultsExist()
    {
        var keys = CloneTools.BuiltIn.Select(t => t.Key).ToList();

        Assert.HasCount(keys.Count, keys.Distinct());
        Assert.IsTrue(CloneTools.DefaultEnabledKeys.All(keys.Contains));
        Assert.IsTrue(CloneTools.BuiltIn.All(t => t.UrlTemplate.Split("{url}").Length == 2));
    }

    [TestMethod]
    public void GetEnabledKeys_NotConfigured_ReturnsDefaults()
    {
        CollectionAssert.AreEqual(CloneTools.DefaultEnabledKeys.ToList(), CloneTools.GetEnabledKeys(null).ToList());
        Assert.IsEmpty(CloneTools.GetEnabledKeys(""));
    }

    [TestMethod]
    public void JoinEnabledKeys_IgnoresUnknownAndEmptyKeys()
    {
        Assert.AreEqual("vscode,rider", CloneTools.JoinEnabledKeys(["", "vscode", "unknown", "rider", "vscode"]));
    }

    [TestMethod]
    public void ParseCustom_ValidLines_ReturnsTools()
    {
        var errors = new List<string>();

        var tools = CloneTools.ParseCustom("Kiro | kiro://vscode.git/clone?url={url}\r\n\r\nWeb|https://example.com/import?repo={url}\n", errors);

        Assert.IsEmpty(errors);
        Assert.HasCount(2, tools);
        Assert.AreEqual("custom:Kiro", tools[0].Key);
        Assert.AreEqual("Kiro", tools[0].Name);
        Assert.AreEqual("kiro://vscode.git/clone?url={url}", tools[0].UrlTemplate);
    }

    [TestMethod]
    [DataRow("NoSeparator")]
    [DataRow("|scheme://x?url={url}")]
    [DataRow("Name|scheme://x")]
    [DataRow("Name|scheme://x?a={url}&b={url}")]
    [DataRow("Name|javascript:alert({url})")]
    [DataRow("Name|DATA:text/html,{url}")]
    [DataRow("Name|file:///{url}")]
    [DataRow("Name|{url}")]
    [DataRow("Name|1abc://x?{url}")]
    public void ParseCustom_InvalidLine_IsRejected(string line)
    {
        var errors = new List<string>();

        var tools = CloneTools.ParseCustom(line, errors);

        Assert.IsEmpty(tools);
        Assert.HasCount(1, errors);
    }

    [TestMethod]
    public void ParseCustom_DuplicateName_IsRejected()
    {
        var errors = new List<string>();

        var tools = CloneTools.ParseCustom("Tool|a://{url}\ntool|b://{url}", errors);

        Assert.HasCount(1, tools);
        Assert.HasCount(1, errors);
    }

    [TestMethod]
    public void ParseCustom_TooManyEntries_KeepsTheFirstAndReportsError()
    {
        var errors = new List<string>();
        var text = string.Join("\n", Enumerable.Range(1, CloneTools.MaxCustomTools + 1).Select(i => $"Tool{i}|tool{i}://{{url}}"));

        var tools = CloneTools.ParseCustom(text, errors);

        Assert.HasCount(CloneTools.MaxCustomTools, tools);
        Assert.HasCount(1, errors);
    }
}
