using Gibbon.Git.Server.Configuration;

namespace Gibbon.Git.Server.Services;

public interface ICloneToolService
{
    IReadOnlyList<CloneTool> GetAvailableTools();

    CloneTool GetPreferredTool();
}
