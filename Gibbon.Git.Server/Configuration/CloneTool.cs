namespace Gibbon.Git.Server.Configuration;

public sealed record CloneTool(string Key, string Name, string UrlTemplate, CloneToolGroup Group = CloneToolGroup.Custom)
{
    public const string UrlPlaceholder = "{url}";

    public string BuildUrl(string gitUrl) => UrlTemplate.Replace(UrlPlaceholder, Uri.EscapeDataString(gitUrl));
}
