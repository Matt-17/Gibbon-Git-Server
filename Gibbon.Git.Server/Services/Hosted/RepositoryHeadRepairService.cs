using System.Threading;
using System.Threading.Tasks;

using Gibbon.Git.Server.Data;
using Gibbon.Git.Server.Repositories;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gibbon.Git.Server.Services.Hosted;

/// <summary>
/// Repairs repositories whose HEAD points to a missing branch, in the background so the start is not delayed.
/// </summary>
public class RepositoryHeadRepairService(ILogger<RepositoryHeadRepairService> logger, IServiceScopeFactory scopeFactory)
    : BackgroundService
{
    private readonly ILogger<RepositoryHeadRepairService> _logger = logger;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repositoryService = scope.ServiceProvider.GetRequiredService<IRepositoryService>();
            var repositoryHeadService = scope.ServiceProvider.GetRequiredService<IRepositoryHeadService>();

            foreach (var repository in repositoryService.GetAllRepositories())
            {
                stoppingToken.ThrowIfCancellationRequested();
                await repositoryHeadService.RepairHeadAsync(repository.Name);
            }
        }
        catch (OperationCanceledException)
        {
            // stopping
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Repairing the HEAD of the repositories failed");
        }
    }
}
