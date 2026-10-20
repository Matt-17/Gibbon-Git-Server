using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gibbon.Git.Server.Git.GitVersionService;

public class GitVersionStartupService(ILogger<GitVersionStartupService> logger, IProcessService processService, IPathResolver pathResolver, IGitVersionService gitVersionSettings, IOptions<GitSettings> gitOptions)
    : IStartupService
{
    private readonly ILogger<GitVersionStartupService> _logger = logger;
    private readonly IProcessService _processService = processService;
    private readonly IPathResolver _pathResolver = pathResolver;
    private readonly IGitVersionService _gitVersionSettings = gitVersionSettings;
    private readonly GitSettings _gitSettings = gitOptions.Value;

    public async Task RunAsync()
    {
        var gitExecutable = _pathResolver.ResolveExecutable(_gitSettings.Executable);
        var version = await IsGitAvailable(gitExecutable);
        if (version is not null)
        {
            _logger.LogInformation("Git {Version} found at {GitExecutable}.", version, gitExecutable);
            _gitVersionSettings.IsGitAvailable = true;
        }
        else
        {
            _logger.LogWarning("Git not found at {GitExecutable}. Install git or set Git:Executable.", gitExecutable);
            _gitVersionSettings.IsGitAvailable = false;
        }
    }

    private async Task<string> IsGitAvailable(string binPath)
    {
        var info = CreateProcessStartInfo("--version", binPath);
        var result = await _processService.StartProcessAsync(info);

        if (!result.IsSuccess)
            return null;

        return StripVersion(result.Output);
    }

    private ProcessStartInfo CreateProcessStartInfo(string args, string gitPath)
    {
        var info = new ProcessStartInfo(gitPath, args)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _pathResolver.GetRepositories()
        };
        return info;
    }

    internal static string StripVersion(string gitVersionOutput)
    {
        var output = gitVersionOutput?.Trim() ?? "";
        var match = Regex.Match(output, @"\d+(\.\d+)*");

        return match.Success ? match.Value : output;
    }
}
