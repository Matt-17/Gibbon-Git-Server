namespace Gibbon.Git.Server.Configuration;

public static class CloneTools
{
    public const string CustomKeyPrefix = "custom:";
    public const int MaxCustomTools = 20;

    private const string JetBrains = "jetbrains://{0}/checkout/git?checkout.repo={{url}}&idea.required.plugins.id=Git4Idea";

    // Only schemes verified against the vendors' sources or documentation.
    public static readonly IReadOnlyList<CloneTool> BuiltIn =
    [
        new("vscode", "Visual Studio Code", "vscode://vscode.git/clone?url={url}"),
        new("visualstudio", "Visual Studio", "git-client://clone?repo={url}"),
        new("cursor", "Cursor", "cursor://vscode.git/clone?url={url}"),
        new("windsurf", "Windsurf", "windsurf://vscode.git/clone?url={url}"),
        new("vscodium", "VSCodium", "vscodium://vscode.git/clone?url={url}"),
        new("idea", "IntelliJ IDEA", string.Format(JetBrains, "idea")),
        new("rider", "Rider", string.Format(JetBrains, "rd")),
        new("webstorm", "WebStorm", string.Format(JetBrains, "web-storm")),
        new("pycharm", "PyCharm", string.Format(JetBrains, "pycharm")),
        new("phpstorm", "PhpStorm", string.Format(JetBrains, "php-storm")),
        new("goland", "GoLand", string.Format(JetBrains, "goland")),
        new("clion", "CLion", string.Format(JetBrains, "clion")),
        new("rubymine", "RubyMine", string.Format(JetBrains, "rubymine")),
        new("rustrover", "RustRover", string.Format(JetBrains, "rustrover")),
        new("tower", "Tower", "gittower://openRepo/{url}"),
        new("sourcetree", "Sourcetree / Fork", "sourcetree://cloneRepo?cloneUrl={url}")
    ];

    public static readonly IReadOnlyList<string> DefaultEnabledKeys = ["vscode", "visualstudio", "cursor", "idea", "rider"];

    private static readonly HashSet<string> ForbiddenSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "javascript", "data", "vbscript", "file", "blob", "filesystem", "about"
    };

    public static IReadOnlyList<string> GetEnabledKeys(string enabledCloneTools)
    {
        return enabledCloneTools is null
            ? DefaultEnabledKeys
            : enabledCloneTools.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public static string JoinEnabledKeys(IEnumerable<string> keys)
    {
        var known = BuiltIn.Select(t => t.Key).ToHashSet();
        return string.Join(',', (keys ?? []).Where(known.Contains).Distinct());
    }

    public static List<CloneTool> ParseCustom(string text, List<string> errors = null)
    {
        var tools = new List<CloneTool>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return tools;
        }

        var lineNumber = 0;
        foreach (var rawLine in text.Split('\n'))
        {
            lineNumber++;
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var separator = line.IndexOf('|');
            var name = separator > 0 ? line[..separator].Trim() : string.Empty;
            var template = separator > 0 ? line[(separator + 1)..].Trim() : string.Empty;
            var error = Validate(name, template, tools);
            if (error is not null)
            {
                errors?.Add(string.Format(Resources.Settings_Global_CloneTools_InvalidLine, lineNumber, error));
                continue;
            }

            tools.Add(new CloneTool(CustomKeyPrefix + name, name, template));
        }

        if (tools.Count > MaxCustomTools)
        {
            errors?.Add(string.Format(Resources.Settings_Global_CloneTools_TooMany, MaxCustomTools));
            tools = tools.Take(MaxCustomTools).ToList();
        }

        return tools;
    }

    private static string Validate(string name, string template, List<CloneTool> existing)
    {
        if (name.Length == 0 || template.Length == 0)
        {
            return Resources.Settings_Global_CloneTools_Format;
        }

        if (existing.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return Resources.Settings_Global_CloneTools_Duplicate;
        }

        var first = template.IndexOf(CloneTool.UrlPlaceholder, StringComparison.Ordinal);
        if (first < 0 || template.IndexOf(CloneTool.UrlPlaceholder, first + 1, StringComparison.Ordinal) >= 0)
        {
            return Resources.Settings_Global_CloneTools_Placeholder;
        }

        var colon = template.IndexOf(':');
        var scheme = colon > 0 ? template[..colon] : string.Empty;
        if (scheme.Length == 0 || !scheme.All(c => char.IsAsciiLetterOrDigit(c) || c is '+' or '-' or '.') || !char.IsAsciiLetter(scheme[0]) || ForbiddenSchemes.Contains(scheme))
        {
            return Resources.Settings_Global_CloneTools_Scheme;
        }

        return null;
    }
}
