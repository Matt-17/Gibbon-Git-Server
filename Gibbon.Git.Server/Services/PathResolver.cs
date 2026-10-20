using Gibbon.Git.Server.Configuration;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace Gibbon.Git.Server.Services;

internal class PathResolver(IWebHostEnvironment hostingEnvironment, IOptions<ApplicationSettings> configuration)
    : IPathResolver
{
    private readonly IWebHostEnvironment _hostingEnvironment = hostingEnvironment;
    private readonly ApplicationSettings _appSettings = configuration.Value;

    public string Resolve(params string[] path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var first = path[0];
        var latter = path[1..];

        if (Path.IsPathRooted(first))
        {
            return ResolveCore(first, latter);
        }

        return ResolveCore(_hostingEnvironment.ContentRootPath, [_appSettings.DataPath, .. path]);
    }
    public string ResolveRoot(string path) => ResolveRoot(_hostingEnvironment.ContentRootPath, path);

    internal static string ResolveRoot(string rootPath, string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return ResolveCore(rootPath, path);
    }

    public string ResolveExecutable(string executable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);

        if (executable.IndexOfAny(['/', '\\']) < 0)
        {
            return executable;
        }

        return ResolveRoot(NormalizeSeparators(executable));
    }

    public string GetRepositories() => ResolveRoot(_appSettings.RepositoryPath);

    public string GetRepoPath(string path, string applicationPath)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (!string.IsNullOrEmpty(applicationPath))
        {
            path = path.Replace(applicationPath, "");
        }

        var gitIndex = path.IndexOf(".git", StringComparison.Ordinal);
        if (gitIndex == -1)
            return path;

        return path[..gitIndex].TrimStart('/');
    }

    public string GetRepositoryPath(string repositoryName)
    {
        ArgumentNullException.ThrowIfNull(repositoryName);

        return Path.Combine(GetRepositories(), repositoryName);
    }

    public string GetRoot()
    {
        return _hostingEnvironment.ContentRootPath;
    }

    internal static string ResolveCore(string rootPath, params string[] paths)
    {
        ArgumentNullException.ThrowIfNull(rootPath);
        ArgumentNullException.ThrowIfNull(paths);

        if (paths.Length == 0)
        {
            return rootPath;
        }

        var appendPath = NormalizeSeparators(paths[0]);

        appendPath = appendPath.TrimStart('~', '/', '\\');

        if (string.IsNullOrEmpty(rootPath))
        {
            rootPath = appendPath;
        }
        else if (!string.IsNullOrEmpty(appendPath))
        {
            rootPath = Path.Combine(rootPath, appendPath);
        }

        rootPath = ResolveCore(rootPath, paths[1..]);

        return rootPath;
    }

    // Configured paths often use Windows separators; on Linux a backslash would become part of the file name.
    private static string NormalizeSeparators(string path)
    {
        var foreignSeparator = Path.DirectorySeparatorChar == '\\' ? '/' : '\\';
        return path?.Replace(foreignSeparator, Path.DirectorySeparatorChar);
    }
}
