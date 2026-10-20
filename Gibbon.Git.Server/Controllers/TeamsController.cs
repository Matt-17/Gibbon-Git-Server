using Gibbon.Git.Server.Data;
using Gibbon.Git.Server.Models;
using Gibbon.Git.Server.Security;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gibbon.Git.Server.Controllers;

[Authorize]
public class TeamsController(IUserService userService, IRepositoryService repositoryService, ITeamService teamRepository)
    : Controller
{
    private readonly IUserService _userService = userService;
    private readonly IRepositoryService _repositoryService = repositoryService;

    private readonly ITeamService _teamRepository = teamRepository;

    public IActionResult Index()
    {
        var teams = _teamRepository
            .GetAllTeams()
            .Select(model => new TeamDetailModel
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                Members = model.Members.ToArray(),
                Repositories = _repositoryService.GetTeamRepositories(model.Id).ToArray(),
            })
            .ToList();

        return View(teams);
    }

    [HttpGet("Teams/{teamname}")]
    public IActionResult Detail(string teamname)
    {
        var team = _teamRepository.GetTeam(teamname);
        if (team == null)
        {
            return NotFound();
        }

        var model = new TeamDetailModel
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            Members = team.Members.ToArray(),
            Repositories = _repositoryService.GetTeamRepositories(team.Id).ToArray(),
        };
        return View(model);
    }

    [HttpGet("Teams/{teamname}/Edit")]
    public IActionResult Edit(string teamname)
    {
        var model = ConvertEditTeamModel(_teamRepository.GetTeam(teamname));
        return View(model);
    }

    [HttpPost("Teams/{teamname}/Edit")]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(TeamEditModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AllUsers = _userService.GetAllUsers().ToArray();
            model.SelectedUsers = model.AllUsers.Where(x => model.PostedSelectedUsers?.Contains(x.Id) == true).ToArray();
            return View(model);
        }

        _teamRepository.Update(ConvertTeamDetailModel(model));
        var team = _teamRepository.GetTeam(model.Id);
        if (team == null)
        {
            return NotFound();
        }

        TempData["UpdateSuccess"] = true;
        return RedirectToAction("Edit", new { teamname = team.Name });
    }

    [HttpGet("Teams/Create")]
    [Authorize(Roles = Roles.Admin)]
    public IActionResult Create()
    {
        var model = new TeamEditModel
        {
            AllUsers = _userService.GetAllUsers().ToArray(),
            SelectedUsers = []
        };
        return View(model);
    }

    [HttpPost("Teams/Create")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public IActionResult Create(TeamEditModel model)
    {
        while (!string.IsNullOrEmpty(model.Name) && model.Name.Last() == ' ')
        {
            model.Name = model.Name[..^1];
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var teammodel = ConvertTeamDetailModel(model);
        if (!_teamRepository.Create(teammodel))
        {
            ModelState.AddModelError("", Resources.Team_Create_Failure);
            return View(model);
        }

        TempData["CreateSuccess"] = true;
        TempData["NewTeamId"] = teammodel.Id;
        return RedirectToAction("Index");
    }

    [HttpGet("Teams/{teamname}/Delete")]
    [Authorize(Roles = Roles.Admin)]
    public IActionResult Delete(string teamname)
    {
        return View(ConvertEditTeamModel(_teamRepository.GetTeam(teamname)));
    }

    [HttpPost("Teams/{teamname}/Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Admin)]
    public IActionResult Delete(TeamEditModel model)
    {
        if (model != null && model.Id != default)
        {
            var team = _teamRepository.GetTeam(model.Id);
            _teamRepository.Delete(team.Id);
            TempData["DeleteSuccess"] = true;
            return RedirectToAction("Index");
        }
        return RedirectToAction("Index");
    }

    private TeamEditModel ConvertEditTeamModel(TeamModel model)
    {
        return model == null ? null : new TeamEditModel
        {
            Id = model.Id,
            Name = model.Name,
            Description = model.Description,
            AllUsers = _userService.GetAllUsers().ToArray(),
            SelectedUsers = model.Members.ToArray(),
        };
    }

    private TeamModel ConvertTeamDetailModel(TeamEditModel model)
    {
        return new TeamModel
        {
            Id = model.Id,
            Name = model.Name,
            Description = model.Description,
            Members = model.PostedSelectedUsers == null ? [] : model.PostedSelectedUsers.Select(x => _userService.GetUserModel(x)).ToArray(),
        };
    }
}
