using Gibbon.Git.Server.Configuration;

namespace Gibbon.Git.Server.Services;

public class CloneToolService(ServerSettings serverSettings, CurrentUserSettings currentUserSettings)
    : ICloneToolService
{
    private readonly ServerSettings _serverSettings = serverSettings;
    private readonly CurrentUserSettings _currentUserSettings = currentUserSettings;
    private IReadOnlyList<CloneTool> _tools;

    public IReadOnlyList<CloneTool> GetAvailableTools()
    {
        if (_tools is not null)
        {
            return _tools;
        }

        var enabled = CloneTools.GetEnabledKeys(_serverSettings.EnabledCloneTools).ToHashSet();
        return _tools = CloneTools.BuiltIn
            .Where(t => enabled.Contains(t.Key))
            .Concat(CloneTools.ParseCustom(_serverSettings.CustomCloneTools))
            .ToList();
    }

    public CloneTool GetPreferredTool()
    {
        var key = _currentUserSettings.Value.PreferredCloneTool;
        return key is null ? null : GetAvailableTools().FirstOrDefault(t => t.Key == key);
    }
}
