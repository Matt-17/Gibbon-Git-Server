namespace Gibbon.Git.Server.Configuration;

public sealed record ApplicationSettings
{
    public bool DemoModeActive { get; set; }
    public const string DefaultDataPath = "App_Data";

    public string DataPath { get; set; } = DefaultDataPath;
    public string RepositoryPath { get; set; }
    public bool LogToFile { get; set; } = true;
    public string LogPath { get; set; }
    public int LogRetainedFileCount { get; set; } = 31;
}
