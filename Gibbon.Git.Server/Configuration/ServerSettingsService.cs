using System.Threading.Tasks;

using Gibbon.Git.Server.Data;
using Gibbon.Git.Server.Data.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Gibbon.Git.Server.Configuration;

public class ServerSettingsService(IMemoryCache memoryCache, GibbonGitServerContext context) : IServerSettingsService
{
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly GibbonGitServerContext _context = context;

    public async Task SaveSettings(ServerSettings settings)
    {
        var entity = await _context.ServerSettings.SingleOrDefaultAsync();

        if (entity == null)
        {
            entity = new ServerSettingsEntity();
            _context.ServerSettings.Add(entity);
        }

        entity.AllowAnonymousPush = settings.AllowAnonymousPush;
        entity.AllowAnonymousRegistration = settings.AllowAnonymousRegistration;
        entity.AllowUserRepositoryCreation = settings.AllowUserRepositoryCreation;
        entity.DefaultLanguage = settings.DefaultLanguage;
        entity.SiteTitle = settings.SiteTitle;
        entity.SiteLogoUrl = settings.SiteLogoUrl;
        entity.SiteCssUrl = settings.SiteCssUrl;
        entity.IsCommitAuthorAvatarVisible = settings.IsCommitAuthorAvatarVisible;
        entity.LinksRegex = settings.LinksRegex;
        entity.LinksUrl = settings.LinksUrl;
        entity.DisplayNameFormat = settings.DisplayNameFormat;
        entity.SortNameFormat = settings.SortNameFormat;
        entity.DateFormat = settings.DateFormat;
        entity.TimeFormat = settings.TimeFormat;
        entity.DefaultRepositoryView = settings.DefaultRepositoryView;
        entity.EnabledCloneTools = settings.EnabledCloneTools;
        entity.CustomCloneTools = settings.CustomCloneTools;

        await _context.SaveChangesAsync();

        _memoryCache.Set("ServerSettings", settings);
    }

    public ServerSettings GetSettings()
    {
        var settings = _context.ServerSettings
            .Select(entity => new ServerSettings
            {
                AllowAnonymousPush = entity.AllowAnonymousPush,
                AllowUserRepositoryCreation = entity.AllowUserRepositoryCreation,
                AllowAnonymousRegistration = entity.AllowAnonymousRegistration,
                DefaultLanguage = entity.DefaultLanguage,
                SiteTitle = entity.SiteTitle,
                SiteLogoUrl = entity.SiteLogoUrl,
                SiteCssUrl = entity.SiteCssUrl,
                IsCommitAuthorAvatarVisible = entity.IsCommitAuthorAvatarVisible,
                LinksRegex = entity.LinksRegex,
                LinksUrl = entity.LinksUrl,
                DisplayNameFormat = entity.DisplayNameFormat,
                SortNameFormat = entity.SortNameFormat,
                DateFormat = entity.DateFormat,
                TimeFormat = entity.TimeFormat,
                DefaultRepositoryView = entity.DefaultRepositoryView,
                EnabledCloneTools = entity.EnabledCloneTools,
                CustomCloneTools = entity.CustomCloneTools
            })
            .SingleOrDefault();

        if (settings == null)
        {
            settings = GetDefaultSettings();
        }

        _memoryCache.Set("ServerSettings", settings);

        return settings;
    }

    public ServerSettings GetDefaultSettings()
    {
        return new ServerSettings
        {
            AllowUserRepositoryCreation = true,
            DefaultLanguage = "en",
        };
    }

    public async Task ResetSettings()
    {
        var defaultSettings = GetDefaultSettings();
        await SaveSettings(defaultSettings);
    }
}
