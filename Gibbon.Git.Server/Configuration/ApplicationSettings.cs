namespace Gibbon.Git.Server.Configuration;

public sealed record ApplicationSettings
{
    public bool DemoModeActive { get; set; }
    public const string DefaultDataPath = "App_Data";

    public string DataPath { get; set; } = DefaultDataPath;
    public string RepositoryPath { get; set; }
}
