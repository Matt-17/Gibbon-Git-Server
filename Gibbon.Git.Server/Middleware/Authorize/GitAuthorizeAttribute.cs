using System.Security.Claims;

using Gibbon.Git.Server.Data;
using Gibbon.Git.Server.Provider;
using Gibbon.Git.Server.Repositories;
using Gibbon.Git.Server.Security;
using Gibbon.Git.Server.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Gibbon.Git.Server.Middleware.Authorize;

public class GitAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    internal const string AuthenticateRealm = "Gibbon Git Server";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var httpContext = context.HttpContext;
        var repositoryPermissionService = context.HttpContext.RequestServices.GetRequiredService<IRepositoryPermissionService>();
        var membershipService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
        var authenticationProvider = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationProvider>();
        var repositoryRepository = context.HttpContext.RequestServices.GetRequiredService<IRepositoryService>();
        var pathResolver = context.HttpContext.RequestServices.GetRequiredService<IPathResolver>();

        httpContext.Request.Headers["AuthNoRedirect"] = "1";

        if (httpContext.User.Identity.IsAuthenticated)
        {
            // User already authenticated
            return;
        }

        var authHeader = httpContext.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrEmpty(authHeader))
        {
            var incomingRepoName = pathResolver.GetRepoPath(httpContext.Request.Path, httpContext.Request.PathBase);
            var repoName = repositoryRepository.NormalizeRepositoryName(incomingRepoName);
            if (repositoryPermissionService.HasPermission(0, repoName, RepositoryAccessLevel.Pull))
                return;
            context.HttpContext.Response.Headers.Append("WWW-Authenticate", $"Basic realm=\"{AuthenticateRealm}\"");
            context.Result = new UnauthorizedResult();
            return;
        }

        if (!IsUserAuthorized(authHeader, httpContext, membershipService, authenticationProvider))
        {
            context.Result = new UnauthorizedResult();
        }
    }

    private bool IsUserAuthorized(string authHeader, HttpContext httpContext, IUserService userService, IAuthenticationProvider authenticationProvider)
    {
        var encodedDataAsBytes = Convert.FromBase64String(authHeader.Replace("Basic ", string.Empty));
        var value = Encoding.ASCII.GetString(encodedDataAsBytes);
        var credentials = value.Split(':', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (credentials.Length != 2)
        {
            return false;
        }

        var username = credentials[0];
        var password = credentials[1];

        if (!userService.IsPasswordValid(username, password))
        {
            return false;
        }

        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(authenticationProvider.GetClaimsForUser(username)));
        return true;
    }
}
