using Gibbon.Git.Server.Services;

using Microsoft.Data.Sqlite;

namespace Gibbon.Git.Server.Configuration;

internal static class DataPathDefaults
{
    public const string RepositoriesFolder = "Repositories";
    public const string GitFolder = "Git";
    public const string DataProtectionKeysFolder = "DataProtection-Keys";
    public const string SqliteFileName = "Gibbon.Git.Server.db";

    public static void Apply(ApplicationSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.DataPath))
        {
            settings.DataPath = ApplicationSettings.DefaultDataPath;
        }

        if (string.IsNullOrWhiteSpace(settings.RepositoryPath))
        {
            settings.RepositoryPath = Path.Combine(settings.DataPath, RepositoriesFolder);
        }
    }

    public static void Apply(GitSettings settings, ApplicationSettings applicationSettings)
    {
        if (string.IsNullOrWhiteSpace(settings.HomePath))
        {
            settings.HomePath = applicationSettings.DataPath;
        }

        if (string.IsNullOrWhiteSpace(settings.BinaryPath))
        {
            settings.BinaryPath = Path.Combine(applicationSettings.DataPath, GitFolder);
        }
    }

    public static string ResolveSqliteConnectionString(string connectionString, string contentRootPath, string dataPath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString ?? "");
        var dataSource = builder.DataSource;

        if (string.IsNullOrWhiteSpace(dataSource))
        {
            builder.DataSource = Path.Combine(dataPath, SqliteFileName);
        }
        else if (dataSource != ":memory:" && !dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase) && !dataSource.StartsWith('|'))
        {
            builder.DataSource = PathResolver.ResolveRoot(contentRootPath, dataSource);
        }

        return builder.ToString();
    }
}
