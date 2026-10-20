using System.Linq;
using System.Reflection;

using Gibbon.Git.Server.Controllers;
using Gibbon.Git.Server.Data;
using Gibbon.Git.Server.Models;
using Gibbon.Git.Server.Security;
using Gibbon.Git.Server.Tests.TestHelper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

namespace Gibbon.Git.Server.Tests.ControllerTests;

[TestClass]
public class TeamsControllerTests : ControllerTestBase<TeamsController>
{
    private ITeamService _teamService = null!;
    private IRepositoryService _repositoryService = null!;
    private IUserService _userService = null!;

    protected override void ConfigureServices(ServiceCollection services)
    {
        _teamService = services.AddSubstitute<ITeamService>();
        _repositoryService = services.AddSubstitute<IRepositoryService>();
        _userService = services.AddSubstitute<IUserService>();
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    public void Detail_TeamNotFound_ReturnsNotFound()
    {
        _teamService.GetTeam(Arg.Any<string>()).Returns((TeamModel)null!);

        var result = Controller.Detail("nonexistent");

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    public void Detail_TeamExists_ReturnsViewWithModel()
    {
        var team = new TeamModel { Id = 1, Name = "dev", Description = "Dev team", Members = [] };
        _teamService.GetTeam("dev").Returns(team);
        _repositoryService.GetTeamRepositories(1).Returns([]);

        var result = Controller.Detail("dev") as ViewResult;

        Assert.IsNotNull(result);
        var model = result.Model as TeamDetailModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("dev", model.Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    public void Edit_Post_ValidModel_RedirectsWithTeamname()
    {
        var team = new TeamModel { Id = 7, Name = "ops", Members = [] };
        _teamService.GetTeam(7).Returns(team);
        _userService.GetAllUsers().Returns([]);

        var model = new TeamEditModel { Id = 7, Name = "ops" };

        var result = Controller.Edit(model) as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Edit", result.ActionName);
        Assert.AreEqual("ops", result.RouteValues?["teamname"]?.ToString());
        Assert.AreEqual(true, Controller.TempData["UpdateSuccess"]);
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    public void Edit_Post_TeamNotFound_ReturnsNotFound()
    {
        _teamService.GetTeam(Arg.Any<int>()).Returns((TeamModel)null!);

        var model = new TeamEditModel { Id = 99, Name = "ghost" };

        var result = Controller.Edit(model);

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    public void Edit_Post_InvalidModel_ReturnsViewWithPostedSelection()
    {
        var alice = new UserModel { Id = 1, Username = "alice" };
        var bob = new UserModel { Id = 2, Username = "bob" };
        _userService.GetAllUsers().Returns([alice, bob]);

        Controller.ModelState.AddModelError("Name", "required");
        var model = new TeamEditModel { Id = 3, PostedSelectedUsers = [2] };

        var result = Controller.Edit(model) as ViewResult;

        Assert.IsNotNull(result);
        var viewModel = result.Model as TeamEditModel;
        Assert.IsNotNull(viewModel);
        Assert.AreEqual(2, viewModel.AllUsers.Length);
        CollectionAssert.AreEqual(new[] { bob }, viewModel.SelectedUsers);
        _teamService.DidNotReceive().Update(Arg.Any<TeamModel>());
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    [DataRow(nameof(TeamsController.Create))]
    [DataRow(nameof(TeamsController.Edit))]
    [DataRow(nameof(TeamsController.Delete))]
    public void ModifyingActions_RequireAdminRole(string actionName)
    {
        var actions = typeof(TeamsController).GetMethods().Where(m => m.Name == actionName).ToList();

        Assert.AreEqual(2, actions.Count);
        foreach (var action in actions)
        {
            var authorize = action.GetCustomAttribute<AuthorizeAttribute>();
            Assert.IsNotNull(authorize, $"{actionName}({string.Join(", ", action.GetParameters().Select(p => p.ParameterType.Name))}) has no [Authorize]");
            Assert.AreEqual(Roles.Admin, authorize.Roles);
        }
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    public void Delete_Post_TeamNotFound_RedirectsWithoutDeleting()
    {
        _teamService.GetTeam(Arg.Any<int>()).Returns((TeamModel)null!);

        var result = Controller.Delete(new TeamEditModel { Id = 42 }) as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Index", result.ActionName);
        _teamService.DidNotReceive().Delete(Arg.Any<int>());
        Assert.IsNull(Controller.TempData["DeleteSuccess"]);
    }
}
