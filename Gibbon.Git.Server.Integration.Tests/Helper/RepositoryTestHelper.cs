using System.Diagnostics;

namespace Gibbon.Git.Server.Integration.Tests.Helper;

internal static class RepositoryTestHelper
{
    public static async Task<string> CreateRepository(HttpClient client, params (string Key, string Value)[] fields)
    {
        var name = "repo" + Guid.NewGuid().ToString("N")[..8];
        var postData = new Dictionary<string, string>(await GetAntiForgeryToken(client, "/Repositories/Create"))
        {
            { "Name", name },
            { "PostedSelectedAdministrators", "1" }
        };
        foreach (var (key, value) in fields)
        {
            postData[key] = value;
        }
        var response = await client.PostAsync("/Repositories/Create", new FormUrlEncodedContent(postData));
        Assert.IsTrue(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect, response.StatusCode.ToString());

        return name;
    }

    public static string GetRepositoryPath(TestContext testContext, string name)
    {
        return Path.Combine(testContext.TestRunDirectory!, "Databases", "Repositories", name);
    }

    public static void PushCommit(TestContext testContext, string repositoryPath, string branch, string message, string? fileName = null)
    {
        var workingDirectory = Path.Combine(testContext.TestRunDirectory!, "work-" + Guid.NewGuid().ToString("N"));
        RunGit(testContext.TestRunDirectory!, "init", "-q", "-b", branch, workingDirectory);
        if (fileName is null)
        {
            RunGit(workingDirectory, "-c", "user.name=Test", "-c", "user.email=test@example.com", "commit", "-q", "--allow-empty", "-m", message);
        }
        else
        {
            File.WriteAllText(Path.Combine(workingDirectory, fileName), message);
            RunGit(workingDirectory, "add", fileName);
            RunGit(workingDirectory, "-c", "user.name=Test", "-c", "user.email=test@example.com", "commit", "-q", "-m", message);
        }
        RunGit(workingDirectory, "tag", "v1");
        RunGit(workingDirectory, "push", "-q", repositoryPath, branch, "v1");
    }

    public static string RunGit(string workingDirectory, params string[] arguments)
    {
        using var process = Process.Start(new ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        })!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.AreEqual(0, process.ExitCode, error);

        return output;
    }
}
