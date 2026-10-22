using System.Diagnostics;
using System.Threading.Tasks;

using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gibbon.Git.Server.Repositories;

public class RepositoryHeadService(ILogger<RepositoryHeadService> logger, IOptions<GitSettings> options, IProcessService processService, IPathResolver pathResolver)
    : IRepositoryHeadService
{
    private readonly ILogger<RepositoryHeadService> _logger = logger;
    private readonly GitSettings _gitSettings = options.Value;
    private readonly IProcessService _processService = processService;
    private readonly IPathResolver _pathResolver = pathResolver;

    public async Task<bool> RepairHeadAsync(string repositoryName)
    {
        var path = Path.GetFullPath(_pathResolver.GetRepositoryPath(repositoryName));

        // newest commit first, the branch HEAD points to is marked with *
        var branches = await RunGit(path, "for-each-ref", "--sort=refname", "--sort=-committerdate", "--format=%(HEAD)%(refname)", "refs/heads");
        if (!branches.IsSuccess)
        {
            return false;
        }

        var lines = branches.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0 || lines.Any(line => line.StartsWith('*')))
        {
            return false;
        }

        // a detached HEAD is left alone, only a branch that does not exist is replaced
        var head = await RunGit(path, "symbolic-ref", "-q", "HEAD");
        if (!head.IsSuccess)
        {
            return false;
        }

        var branch = lines[0];

        var result = await RunGit(path, "symbolic-ref", "HEAD", branch);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Could not point HEAD of {RepositoryName} to {Branch}: {Error}", repositoryName, branch, result.Error);
            return false;
        }

        _logger.LogWarning("HEAD of {RepositoryName} pointed to the missing branch {OldHead}, now points to {Branch}", repositoryName, head.Output.Trim(), branch);
        return true;
    }

    private async Task<ProcessResult> RunGit(string repositoryPath, params string[] arguments)
    {
        var info = new ProcessStartInfo(_pathResolver.ResolveExecutable(_gitSettings.Executable), ["-C", repositoryPath, .. arguments])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _pathResolver.GetRepositories()
        };
        info.EnvironmentVariables["HOME"] = _pathResolver.ResolveRoot(_gitSettings.HomePath);

        return await _processService.StartProcessAsync(info);
    }
}
