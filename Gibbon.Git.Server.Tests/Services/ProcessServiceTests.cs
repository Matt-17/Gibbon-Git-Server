using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Gibbon.Git.Server.Services;

namespace Gibbon.Git.Server.Tests.Services;

[TestClass]
[TestCategory(TestCategories.UnitTest)]
public class ProcessServiceTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ProcessExitsWithoutReadingInput_OutputIsStillCopied(bool endStreamWithClose)
    {
        var info = new ProcessStartInfo("git", "--version")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var input = new MemoryStream(new byte[4 * 1024 * 1024]);
        using var output = new MemoryStream();

        var result = await new ProcessService().StartProcessWithStreamAsync(info, input, output, endStreamWithClose);

        Assert.IsTrue(result.IsSuccess, result.Error);
        StringAssert.StartsWith(Encoding.UTF8.GetString(output.ToArray()), "git version");
    }
}
