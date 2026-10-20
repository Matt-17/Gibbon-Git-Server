using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Data;
using Gibbon.Git.Server.Git.GitDownloadService;
using Gibbon.Git.Server.Git.GitService;
using Gibbon.Git.Server.Git.GitVersionService;
using Gibbon.Git.Server.Middleware;
using Gibbon.Git.Server.Provider;
using Gibbon.Git.Server.Repositories;
using Gibbon.Git.Server.Security;
using Gibbon.Git.Server.Services;
using Gibbon.Git.Server.Services.Hosted;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var config = builder.Configuration;
var env = builder.Environment;

config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

var databaseSection = config.GetSection("Database");
services.Configure<DatabaseSettings>(databaseSection);
services.Configure<ApplicationSettings>(config.GetSection("Application"));
services.PostConfigure<ApplicationSettings>(DataPathDefaults.Apply);
services.Configure<GitSettings>(config.GetSection("Git"));
services.AddOptions<GitSettings>().PostConfigure<IOptions<ApplicationSettings>>((git, application) => DataPathDefaults.Apply(git, application.Value));
services.Configure<MailSettings>(config.GetSection("Mail"));

var applicationSettings = config.GetSection("Application").Get<ApplicationSettings>() ?? new ApplicationSettings();
DataPathDefaults.Apply(applicationSettings);
var dataPath = PathResolver.ResolveRoot(env.ContentRootPath, applicationSettings.DataPath);

var dataProtection = services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataPath, DataPathDefaults.DataProtectionKeysFolder)))
    .SetApplicationName("Gibbon.Git.Server");
if (OperatingSystem.IsWindows())
{
    dataProtection.ProtectKeysWithDpapi(protectToLocalMachine: true);
}

services.AddScoped(serviceProvider => serviceProvider.GetRequiredService<IServerSettingsService>().GetSettings());

var databaseProvider = databaseSection.GetValue<DatabaseProviderTypes>("Provider");
var connectionString = databaseSection.GetConnectionString($"{databaseProvider}") ?? databaseSection.GetConnectionString("Default");

DbContextOptionsBuilder ConfigureOptions(DbContextOptionsBuilder options)
{
    options.ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
    return options;
}

switch (databaseProvider)
{
    case DatabaseProviderTypes.Memory:
        services.AddDbContext<GibbonGitServerContext, SqlServerGibbonContext>(options =>
        {
            ConfigureOptions(options).UseInMemoryDatabase(connectionString ?? "Data Source=InMemory;");
        });
        break;
    case DatabaseProviderTypes.Sqlite:
        var sqliteConnectionString = DataPathDefaults.ResolveSqliteConnectionString(connectionString, env.ContentRootPath, dataPath);
        var sqliteDirectory = Path.GetDirectoryName(new SqliteConnectionStringBuilder(sqliteConnectionString).DataSource);
        if (!string.IsNullOrEmpty(sqliteDirectory))
        {
            Directory.CreateDirectory(sqliteDirectory);
        }

        services.AddDbContext<GibbonGitServerContext, SqliteGibbonContext>(options =>
        {
            ConfigureOptions(options).UseSqlite(sqliteConnectionString);
        });
        break;
    case DatabaseProviderTypes.SqlServer:
        services.AddDbContext<GibbonGitServerContext, SqlServerGibbonContext>(options =>
        {
            ConfigureOptions(options).UseSqlServer(connectionString);
        });
        break;
}

services.AddLogging();
services.AddMemoryCache();

services.AddSingleton<IPathResolver, PathResolver>();
services.AddSingleton<IGitVersionService, GitVersionService>();
//services.AddSingleton<ICertificateService, CertificateService>();

services.AddScoped<IDatabaseHelperService, DatabaseHelperService>();
services.AddScoped<IPasswordService, PasswordService>();
services.AddScoped<ICultureService, CultureService>();
services.AddScoped<IUserService, UserService>();
services.AddScoped<IRoleProvider, RoleProvider>();
services.AddScoped<IAuthenticationProvider, CookieAuthenticationProvider>();
services.AddScoped<ITeamService, TeamService>();
services.AddScoped<IRepositoryService, RepositoryService>();
services.AddScoped<IRepositoryPermissionService, RepositoryPermissionService>();
services.AddScoped<IDiagnosticReporter, DiagnosticReporter>();
services.AddScoped<IRepositorySynchronizer, RepositorySynchronizer>();
services.AddScoped<IMailService, MailService>();
services.AddScoped<IServerSettingsService, ServerSettingsService>();
services.AddScoped<IUserSettingsService, UserSettingsService>();
services.AddScoped<CurrentUserSettings>();
services.AddScoped<IUserNameFormatter, UserNameFormatter>();
services.AddScoped<IDateFormatter, DateFormatter>();

services.AddScoped<IProcessService, ProcessService>();
services.AddScoped<IGitService, GitService>();
services.AddScoped<IGitDownloadService, GitDownloadService>();

services.AddScoped<IAvatarService, AvatarService>();
services.AddSingleton<IRepositoryLogoService, RepositoryLogoService>();
services.AddScoped<IRepositoryBrowserFactory, RepositoryBrowserFactory>();
services.AddScoped<IRepositoryCopier, RepositoryCopier>();
services.AddScoped<IAuthorizationHandler, RepositoryPermissionHandler>();

services.AddScoped<IUserOutputService, UserOutputService>();
services.AddTransient<IRepositoryBrowser, RepositoryBrowser>();

services.AddTransient<IStartupService, RepositoryStartupService>();
services.AddTransient<IStartupService, GitVersionStartupService>();

services.AddHostedService<DatabaseMigrationService>();
services.AddHostedService<StartupServiceRunner>();

services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();

services.AddHttpClient<IGitDownloadService, GitDownloadService>();

services.AddScoped<NormalizeRepositoryNameFilter>();
services.AddHttpContextAccessor();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024;
});

services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024;
});

services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "AntiForgery";
    options.FormFieldName = "_AntiForgery";
});

services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Error";
        options.ExpireTimeSpan = TimeSpan.FromDays(3);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToAccessDenied = async context =>
        {
            context.Response.StatusCode = 403;
        };
        options.Events.OnRedirectToLogin = async context =>
        {
            if (!context.Request.Headers.ContainsKey("AuthNoRedirect"))
            {
                context.Response.Redirect(context.RedirectUri);
            }
        };
    });

services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.RepositoryAdmin, policy => policy.Requirements.Add(new RepositoryPermissionRequirement(RepositoryAccessLevel.Administer)));
    options.AddPolicy(Policies.RepositoryPull, policy => policy.Requirements.Add(new RepositoryPermissionRequirement(RepositoryAccessLevel.Pull)));
    options.AddPolicy(Policies.RepositoryPush, policy => policy.Requirements.Add(new RepositoryPermissionRequirement(RepositoryAccessLevel.Push)));
});

services.AddControllersWithViews()
    .AddCookieTempDataProvider();

#if DEBUG         
if (env.IsDevelopment())
{
    services.AddSassCompiler();
}
#endif

var app = builder.Build();

app.UseStatusCodePagesWithReExecute("/Error/{0}");
app.UseExceptionHandler("/Error");

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = new FileExtensionContentTypeProvider
    {
        Mappings = { [".woff2"] = "font/woff2" }
    }
});

app.UseCookiePolicy();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<CultureMiddleware>();

app.MapControllers();

app.MapControllerRoute(
    name: "Default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();
