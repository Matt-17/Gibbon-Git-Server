using System.Diagnostics;
using System.Threading.Tasks;

using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gibbon.Git.Server.Repositories;

public class RepositoryCopier(ILogger<RepositoryCopier> logger, IOptions<GitSettings> options, IProcessService processService, IPathResolver pathResolver)
    : IRepositoryCopier
{
    private readonly ILogger<RepositoryCopier> _logger = logger;
    private readonly GitSettings _gitSettings = options.Value;
    private readonly IProcessService _processService = processService;
    private readonly IPathResolver _pathResolver = pathResolver;

    public async Task<bool> CopyAsync(string sourcePath, string targetPath)
    {
        var success = await RunGit("clone", "--mirror", "--no-hardlinks", "--quiet", Path.GetFullPath(sourcePath), Path.GetFullPath(targetPath))
            && await RunGit("-C", Path.GetFullPath(targetPath), "config", "--remove-section", "remote.origin");

        if (!success && Directory.Exists(targetPath))
        {
            foreach (var file in Directory.EnumerateFiles(targetPath, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            Directory.Delete(targetPath, true);
        }

        return success;
    }

    private async Task<bool> RunGit(params string[] arguments)
    {
        var info = new ProcessStartInfo(_pathResolver.ResolveExecutable(_gitSettings.Executable), arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _pathResolver.GetRepositories()
        };
        info.EnvironmentVariables["HOME"] = _pathResolver.ResolveRoot(_gitSettings.HomePath);

        var result = await _processService.StartProcessAsync(info);
        if (!result.IsSuccess)
        {
            _logger.LogError("git {Arguments} failed: {Error}", string.Join(' ', arguments), result.Error);
        }

        return result.IsSuccess;
    }
}
