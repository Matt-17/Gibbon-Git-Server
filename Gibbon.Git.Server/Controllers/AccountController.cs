using System.Threading.Tasks;

using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Extensions;
using Gibbon.Git.Server.Models;
using Gibbon.Git.Server.Security;
using Gibbon.Git.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Gibbon.Git.Server.Controllers;

[Authorize]
public class AccountController(IUserService userService, IRoleProvider roleProvider, ICultureService cultureService, IUserSettingsService userSettingsService)
    : Controller
{
    private readonly IUserService _userService = userService;
    private readonly IRoleProvider _roleProvider = roleProvider;
    private readonly ICultureService _cultureService = cultureService;
    private readonly IUserSettingsService _userSettingsService = userSettingsService;

    public UserModel UserModel { get; set; }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var username = User.Identity.Name;
        UserModel = _userService.GetUserModel(username);

        if (UserModel == null)
        {
            context.Result = BadRequest();
            return;
        }

        await base.OnActionExecutionAsync(context, next);
    }
    [HttpGet]
    public IActionResult Index()
    {
        var model = new MeDetailModel
        {
            Username = UserModel.Username,
            Name = UserModel.GivenName,
            Surname = UserModel.Surname,
            Email = UserModel.Email,
            Roles = _roleProvider.GetRolesForUser(UserModel.Id)
        };
        return View(model);
    }

    [HttpGet]
    public IActionResult Edit()
    {
        var model = new MeEditModel
        {
            Username = UserModel.Username,
            Name = UserModel.GivenName,
            Surname = UserModel.Surname,
            Email = UserModel.Email,
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(MeEditModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _userService.UpdateUser(UserModel.Id, model.Name, model.Surname, model.Email);

        TempData["EditSuccess"] = true;
        return RedirectToAction("Edit");
    }

    [HttpGet]
    public IActionResult Password()
    {
        var model = new MePasswordModel();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Password(MePasswordModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (string.Equals(model.OldPassword, model.NewPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(model.NewPassword), Resources.MeController_Password_MustBeDifferent);
            return View(model);
        }

        var username = User.Identity.Name;

        if (!_userService.IsPasswordValid(username, model.OldPassword))
        {
            ModelState.AddModelError(nameof(model.OldPassword), Resources.Account_Edit_OldPasswordIncorrect);
            return View(model);
        }

        var userId = User.Id();
        _userService.UpdatePassword(userId, model.NewPassword);
        
        TempData["PasswordChangeSuccess"] = true;
        return RedirectToAction("Password");
    }

    [HttpGet]
    public async Task<IActionResult> Settings()
    {
        var cultures = await _cultureService.GetSupportedCultures();

        var cultureItems = cultures
            .Select(cultureInfo => new SelectListItem
            {
                Text = $"{cultureInfo.Name} - {cultureInfo.DisplayName}",
                Value = cultureInfo.Name
            })
            .ToList();

        cultureItems.Insert(0, new SelectListItem
        {
            Text = Resources.MeController_Settings_UseServerLanguage,
            Value = ""
        });

        var settings = await _userSettingsService.GetSettings(UserModel.Id);

        return View(new MeSettingsModel
        {
            PreferredLanguage = settings.PreferredLanguage,
            DisplayNameFormat = settings.DisplayNameFormat,
            SortNameFormat = settings.SortNameFormat,
            AvailableLanguages = cultureItems
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(MeSettingsModel settings)
    {
        if (!ModelState.IsValid)
        {
            var cultures = await _cultureService.GetSupportedCultures();
            settings.AvailableLanguages = cultures
                .Select(cultureInfo => new SelectListItem
                {
                    Text = $"{cultureInfo.Name} - {cultureInfo.DisplayName}",
                    Value = cultureInfo.Name
                })
                .ToList();

            return View(settings);
        }

        await _userSettingsService.SaveSettings(UserModel.Id, new UserSettings
        {
            PreferredLanguage = settings.PreferredLanguage,
            DisplayNameFormat = settings.DisplayNameFormat,
            SortNameFormat = settings.SortNameFormat
        });

        return RedirectToAction("Settings");
    }
}
