using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Data;
using Gibbon.Git.Server.Extensions;
using Gibbon.Git.Server.Git.GitService;
using Gibbon.Git.Server.Middleware.Authorize;
using Gibbon.Git.Server.Repositories;
using Gibbon.Git.Server.Security;
using Gibbon.Git.Server.Services;

using ICSharpCode.SharpZipLib.GZip;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Repository = LibGit2Sharp.Repository;

namespace Gibbon.Git.Server.Controllers;

[GitAuthorize]
public class GitController(ILogger<GitController> logger, IRepositoryPermissionService repositoryPermissionService, IRepositoryService repositoryService, IUserService userService, IGitService gitService, ServerSettings serverOptions, IPathResolver pathResolver)
    : ControllerBase
{
    private readonly ServerSettings _serverSettings = serverOptions;
    private readonly ILogger<GitController> _logger = logger;
    private readonly IRepositoryPermissionService _repositoryPermissionService = repositoryPermissionService;
    private readonly IRepositoryService _repositoryService = repositoryService;
    private readonly IUserService _userService = userService;
    private readonly IGitService _gitService = gitService;
    private readonly IPathResolver _pathResolver = pathResolver;

    [HttpGet("{repositoryName}.git/info/refs")]
    public IActionResult SecureGetInfoRefs(string repositoryName, string service)
    {
        repositoryName = _repositoryService.NormalizeRepositoryName(repositoryName);
        if (!RepositoryIsValid(repositoryName))
        {
            return GitNotFound();
        }
        var isPush = string.Equals("git-receive-pack", service, StringComparison.OrdinalIgnoreCase);
        var requiredLevel = isPush ? RepositoryAccessLevel.Push : RepositoryAccessLevel.Pull;
        if (!_repositoryPermissionService.HasPermission(User.Id(), repositoryName, requiredLevel))
        {
            _logger.LogWarning("SecureGetInfoRefs unauth because User {UserId} doesn't have permission {Permission} on  repo {RepositoryName}", User.Id(), requiredLevel, repositoryName);
            return GitForbid();
        }

        string input = $"# service={service}\n";
        var formattedServiceMessage = $"{(input.Length + 4).ToString("X").PadLeft(4, '0')}{input}0000";

        return new GitCmdResult(
            $"application/x-{service}-advertisement",
            async outStream => await _gitService.ExecuteServiceByName(repositoryName,
                service[4..],
                new ExecutionOptions(true),
                GetInputStream(),
                outStream,
                HttpContext.User.Id()
            ),
            formattedServiceMessage);
    }

    [HttpPost("{repositoryName}.git/git-upload-pack")]
    public IActionResult SecureUploadPack(string repositoryName)
    {
        repositoryName = _repositoryService.NormalizeRepositoryName(repositoryName);
        if (!RepositoryIsValid(repositoryName))
        {
            return GitNotFound();
        }

        if (!_repositoryPermissionService.HasPermission(User.Id(), repositoryName, RepositoryAccessLevel.Pull))
        {
            return GitForbid();
        }

        return new GitCmdResult(
            "application/x-git-upload-pack-result",
            async outStream => await _gitService.ExecuteServiceByName(repositoryName,
                "upload-pack",
                new ExecutionOptions(false, true),
                GetInputStream(),
                outStream,
                HttpContext.User.Id()
            )
        );
    }

    [HttpPost("{repositoryName}.git/git-receive-pack")]
    public IActionResult SecureReceivePack(string repositoryName)
    {
        repositoryName = _repositoryService.NormalizeRepositoryName(repositoryName);
        if (!RepositoryIsValid(repositoryName))
        {
            return GitNotFound();
        }

        if (!_repositoryPermissionService.HasPermission(User.Id(), repositoryName, RepositoryAccessLevel.Push))
        {
            return GitForbid();
        }

        return new GitCmdResult(
            "application/x-git-receive-pack-result",
            async outStream => await _gitService.ExecuteServiceByName(repositoryName,
                "receive-pack",
                new ExecutionOptions(false),
                GetInputStream(),
                outStream,
                HttpContext.User.Id()
            )
        );
    }

    /// <summary>
    /// Action to handle .git URLs by redirecting to the repository details page.
    /// </summary>
    [HttpGet("{repositoryName}.git")]
    public IActionResult GitUrl(string repositoryName)
    {
        if (string.IsNullOrEmpty(repositoryName))
        {
            return BadRequest("Repository name is required.");
        }

        return RedirectToAction("Open", "Repositories", new { name = repositoryName });
    }

    /// <summary>
    /// Redirect requests for info/refs without .git to the .git URL
    /// </summary>
    [HttpGet("{repositoryName}/info/refs")]
    public IActionResult RedirectInfoRefs(string repositoryName)
    {
        var queryString = HttpContext.Request.QueryString.HasValue 
            ? HttpContext.Request.QueryString.Value 
            : string.Empty;
        var newPath = $"{HttpContext.Request.PathBase}/{repositoryName}.git/info/refs{queryString}";
        return Redirect(newPath);
    }

    /// <summary>
    /// Redirect requests for git-upload-pack without .git to the .git URL
    /// </summary>
    [HttpPost("{repositoryName}/git-upload-pack")]
    public IActionResult RedirectUploadPack(string repositoryName)
    {
        var newPath = $"{HttpContext.Request.PathBase}/{repositoryName}.git/git-upload-pack";
        return RedirectPreserveMethod(newPath);
    }

    /// <summary>
    /// Redirect requests for git-receive-pack without .git to the .git URL
    /// </summary>
    [HttpPost("{repositoryName}/git-receive-pack")]
    public IActionResult RedirectReceivePack(string repositoryName)
    {
        var newPath = $"{HttpContext.Request.PathBase}/{repositoryName}.git/git-receive-pack";
        return RedirectPreserveMethod(newPath);
    }

    private bool RepositoryIsValid(string repositoryName)
    {
        var directory = _pathResolver.GetRepositoryPath(repositoryName);
        var isValid = Repository.IsValid(directory);
        if (!isValid)
        {
            _logger.LogWarning("Invalid repo {RepositoryName}", repositoryName);
        }
        return isValid;
    }

    private Stream GetInputStream()
    {
        // For really large uploads we need to get a bufferless input stream and disable the max
        // request length.
        var requestStream = HttpContext.Request.Body;

        return Request.Headers["Content-Encoding"] == "gzip"
            ? new GZipInputStream(requestStream)
            : requestStream;
    }

    /// <summary>
    /// Returns a plain text response with status code 401 for anonymous requests, otherwise 403 (Forbidden).
    /// git only sends credentials after a 401, e.g. when pushing to a repository that allows anonymous pull.
    /// </summary>
    /// <returns>A plain text response indicating the request is not allowed.</returns>
    private IActionResult GitForbid()
    {
        if (User.Id() == 0)
        {
            Response.Headers.Append("WWW-Authenticate", $"Basic realm=\"{GitAuthorizeAttribute.AuthenticateRealm}\"");
            Response.StatusCode = 401;
            return Content("Authentication required.", "text/plain; charset=UTF-8");
        }

        Response.StatusCode = 403;
        return Content("Forbidden.", "text/plain; charset=UTF-8");
    }

    /// <summary>
    /// Returns a plain text response with status code 404 (Not Found).
    /// </summary>
    /// <returns>A plain text response indicating the repository was not found.</returns>
    private IActionResult GitNotFound()
    {
        Response.StatusCode = 404;
        return Content("Repository not found.", "text/plain; charset=UTF-8");
    }
}
