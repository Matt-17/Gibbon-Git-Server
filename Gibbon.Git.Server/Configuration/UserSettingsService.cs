using System.Threading.Tasks;

using Gibbon.Git.Server.Data;
using Gibbon.Git.Server.Data.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Gibbon.Git.Server.Configuration;

public class UserSettingsService(IMemoryCache memoryCache, GibbonGitServerContext context) : IUserSettingsService
{
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly GibbonGitServerContext _context = context;

    public async Task SaveSettings(int userId, UserSettings settings)
    {
        var entity = await _context.UserSettings.SingleOrDefaultAsync(u => u.UserId == userId);

        if (entity == null)
        {
            entity = new UserSettingsEntity { UserId = userId };
            _context.UserSettings.Add(entity);
        }

        entity.PreferredLanguage = settings.PreferredLanguage;
        entity.DisplayNameFormat = settings.DisplayNameFormat;
        entity.SortNameFormat = settings.SortNameFormat;
        entity.DateFormat = settings.DateFormat;
        entity.TimeFormat = settings.TimeFormat;
        entity.DefaultRepositoryView = settings.DefaultRepositoryView;
        entity.PreferredCloneTool = settings.PreferredCloneTool;

        await _context.SaveChangesAsync();

        _memoryCache.Set($"UserSettings_{userId}", settings);
    }

    public async Task<UserSettings> GetSettings(int userId)
    {
        ArgumentNullException.ThrowIfNull(userId, nameof(userId));

        if (_memoryCache.TryGetValue($"UserSettings_{userId}", out UserSettings cachedSettings))
        {
            return cachedSettings;
        }

        var settings = await _context.UserSettings
            .Where(u => u.UserId == userId)
            .Select(entity => new UserSettings
            {
                PreferredLanguage = entity.PreferredLanguage,
                DisplayNameFormat = entity.DisplayNameFormat,
                SortNameFormat = entity.SortNameFormat,
                DateFormat = entity.DateFormat,
                TimeFormat = entity.TimeFormat,
                DefaultRepositoryView = entity.DefaultRepositoryView,
                PreferredCloneTool = entity.PreferredCloneTool
            })
            .SingleOrDefaultAsync();

        settings ??= GetDefaultSettings();

        _memoryCache.Set($"UserSettings_{userId}", settings);

        return settings;
    }

    public UserSettings GetDefaultSettings()
    {
        return new UserSettings
        {
            PreferredLanguage = null
        };
    }
}
