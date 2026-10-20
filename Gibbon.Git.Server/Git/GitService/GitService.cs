using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading.Tasks;

using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Data;
using Gibbon.Git.Server.Extensions;
using Gibbon.Git.Server.Security;
using Gibbon.Git.Server.Services;

using Microsoft.Extensions.Options;

namespace Gibbon.Git.Server.Git.GitService;

public class GitService(IOptions<GitSettings> options, IProcessService processService, IPathResolver pathResolver, ITeamService teamService, IRoleProvider roleProvider, IUserService userService)
    : IGitService
{
    private static readonly HashSet<string> PermittedServiceNames = ["upload-pack", "receive-pack"];
    private readonly string _gitExecutable = options.Value.Executable;
    private readonly string _gitHomePath = options.Value.HomePath;
    private readonly IProcessService _processService = processService;
    private readonly IPathResolver _pathResolver = pathResolver;
    private readonly ITeamService _teamService = teamService;
    private readonly IRoleProvider _roleProvider = roleProvider;
    private readonly IUserService _userService = userService;

    public async Task ExecuteServiceByName(string repositoryName, string serviceName, ExecutionOptions options, Stream inStream, Stream outStream, int userId)
    {
        if (!PermittedServiceNames.Contains(serviceName))
        {
            throw new InvalidOperationException("Invalid service name.");
        }

        var args = BuildArgs(repositoryName, serviceName, options);
        var info = CreateProcessStartInfo(_pathResolver.ResolveExecutable(_gitExecutable), args);

        SetEnvironmentVariables(userId, info.EnvironmentVariables);

        await _processService.StartProcessWithStreamAsync(info, inStream, outStream, options.EndStreamWithClose);
    }

    private string BuildArgs(string repositoryName, string serviceName, ExecutionOptions options)
    {
        var args = new List<string>
        {
            serviceName,
            "--stateless-rpc"
        };

        if (options.AdvertiseRefs)
        {
            args.Add("--advertise-refs");
        }

        args.Add($"\"{_pathResolver.GetRepositoryPath(repositoryName)}\"");

        return string.Join(" ", args);
    }

    private ProcessStartInfo CreateProcessStartInfo(string gitPath, string args) => new(gitPath, args)
    {
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
        WorkingDirectory = _pathResolver.GetRepositories()
    };

    private void SetEnvironmentVariables(int userId, StringDictionary environmentVariables)
    {
        environmentVariables["HOME"] = _pathResolver.ResolveRoot(_gitHomePath);

        var teamsstr = string.Empty;
        var rolesstr = string.Empty;
        var displayname = string.Empty;
        var username = string.Empty;

        if (userId != 0)
        {
            teamsstr = _teamService.GetTeamsForUser(userId)
                .Select(x => x.Name)
                .StringlistToEscapedStringForEnvVar();

            rolesstr = _roleProvider.GetRolesForUser(userId)
                .StringlistToEscapedStringForEnvVar();

            var userModel = _userService.GetUserModel(userId);
            displayname = userModel.DisplayName;
            username = userModel.Username;
        }

        environmentVariables["AUTH_USER"] = username;
        environmentVariables["REMOTE_USER"] = username;
        environmentVariables["AUTH_USER_TEAMS"] = teamsstr;
        environmentVariables["AUTH_USER_ROLES"] = rolesstr;
        environmentVariables["AUTH_USER_DISPLAYNAME"] = displayname;
    }
}
