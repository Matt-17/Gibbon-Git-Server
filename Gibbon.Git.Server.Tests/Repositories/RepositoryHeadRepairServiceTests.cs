using System.Threading;
using System.Threading.Tasks;

using Gibbon.Git.Server.Data;
using Gibbon.Git.Server.Models;
using Gibbon.Git.Server.Repositories;
using Gibbon.Git.Server.Services.Hosted;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Gibbon.Git.Server.Tests.Repositories;

[TestClass]
[TestCategory(TestCategories.UnitTest)]
public class RepositoryHeadRepairServiceTests
{
    [TestMethod]
    public async Task Start_RepairsHeadOfEveryRepository()
    {
        var repositoryService = Substitute.For<IRepositoryService>();
        repositoryService.GetAllRepositories().Returns([new RepositoryModel { Name = "alpha" }, new RepositoryModel { Name = "beta" }]);
        var headService = Substitute.For<IRepositoryHeadService>();
        var services = new ServiceCollection()
            .AddSingleton(repositoryService)
            .AddSingleton(headService)
            .BuildServiceProvider();
        var service = new RepositoryHeadRepairService(NullLogger<RepositoryHeadRepairService>.Instance, services.GetRequiredService<IServiceScopeFactory>());

        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!;

        await headService.Received(1).RepairHeadAsync("alpha");
        await headService.Received(1).RepairHeadAsync("beta");
    }
}
