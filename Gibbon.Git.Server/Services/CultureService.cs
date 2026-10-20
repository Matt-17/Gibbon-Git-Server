using System.Globalization;
using System.Threading.Tasks;

using Gibbon.Git.Server.Configuration;

namespace Gibbon.Git.Server.Services;

internal sealed class CultureService(ServerSettings serverSettings, IUserSettingsService userSettingsService)
    : ICultureService
{
    private const string DefaultLanguage = "en-US";

    private readonly ServerSettings _serverSettings = serverSettings;
    private readonly IUserSettingsService _userSettingsService = userSettingsService;

    public async Task<CultureInfo> GetSelectedCultureInfo(int userId)
    {
        var userSettings = userId != 0 ? await _userSettingsService.GetSettings(userId) : _userSettingsService.GetDefaultSettings();

        var language = userSettings.PreferredLanguage ?? _serverSettings.DefaultLanguage ?? DefaultLanguage;

        return new CultureInfo(language);
    }

    public Task<List<CultureInfo>> GetSupportedCultures() => Task.FromResult(GetSupportedCultures(AppContext.BaseDirectory));

    internal static List<CultureInfo> GetSupportedCultures(string applicationDirectory)
    {
        var satelliteAssembly = typeof(Resources).Assembly.GetName().Name + ".resources.dll";

        return Directory.GetDirectories(applicationDirectory)
            .Where(directory => File.Exists(Path.Combine(directory, satelliteAssembly)))
            .Select(Path.GetFileName)
            .Append("en")
            .Select(CultureInfo.GetCultureInfo)
            .OrderBy(i => i.Name)
            .ToList();
    }
}
