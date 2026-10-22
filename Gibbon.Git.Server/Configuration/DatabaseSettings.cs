namespace Gibbon.Git.Server.Configuration;

public sealed record DatabaseSettings
{
    public DatabaseProviderTypes DatabaseProvider { get; set; }
    public bool AllowMigration { get; set; } = true;
}
