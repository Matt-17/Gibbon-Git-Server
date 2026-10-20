using System.Diagnostics;

using Gibbon.Git.Server.Git.GitDownloadService;
using Gibbon.Git.Server.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gibbon.Git.Server.Integration.Tests.Helper;

internal class GibbonGitServerApplication : WebApplicationFactory<Program>
{
    private readonly string _fileName;
    public IMailService MailService { get; init; }
    public IGitDownloadService GitDownloadService { get; set; }

    public GibbonGitServerApplication(TestContext testContext)
    {
        Debug.Assert(testContext.TestRunDirectory != null, "TestContext.TestRunDirectory != null");
        _fileName = Path.Combine(testContext.TestRunDirectory, "Databases", testContext.TestName + ".db");
        Debug.WriteLine($"Database file: {_fileName}");

        var directory = Path.GetDirectoryName(_fileName);

        Directory.CreateDirectory(directory!);

        if (File.Exists(_fileName))
        {
            File.Delete(_fileName);
        }

        MailService = Substitute.For<IMailService>();
        GitDownloadService = Substitute.For<IGitDownloadService>();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureHostConfiguration(config =>
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Application:DataPath", Path.GetDirectoryName(_fileName) },
                { "Database:Provider", "Memory" },
                { "Database:AllowMigration", "true" },
                { "Database:ConnectionStrings:Memory", $"Data Source={_fileName}" }
            };
            config.AddInMemoryCollection(inMemorySettings);

        });
        builder.ConfigureServices(services =>
        {
            services.AddScoped(_ => MailService);
            services.AddScoped(_ => GitDownloadService);
        });
        var host = base.CreateHost(builder);
        return host;
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {                 
        //builder.ConfigureServices(services =>
        //{
        //    services.AddScoped(_ => MailService);
        //    services.AddScoped(_ => GitDownloadService);
        //});
        base.ConfigureWebHost(builder);
    }
}
