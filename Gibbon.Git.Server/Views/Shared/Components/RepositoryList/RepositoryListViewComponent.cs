using Gibbon.Git.Server.Extensions;
using Gibbon.Git.Server.Models;
using Gibbon.Git.Server.Security;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;
using Gibbon.Git.Server.Helpers;
using Gibbon.Git.Server.Repositories;

namespace Gibbon.Git.Server.Views.Shared.Components.RepositoryList;

public class RepositoryListViewComponent(IRepositoryPermissionService repoPermissions, IUrlHelperFactory urlHelperFactory)
    : ViewComponent
{
    private readonly IRepositoryPermissionService _repoPermissions = repoPermissions;
    private readonly IUrlHelperFactory _urlHelperFactory = urlHelperFactory;

    public IViewComponentResult Invoke()
    {
        if (!User.Identity.IsAuthenticated)
        {
            return View(new List<SelectListItem>());
        }

        var userId = User.Id();
        var currentRepositoryId = ViewContext.RouteData.Values["name"] as string;        

        var items = PopulateRepoGoToList(userId, currentRepositoryId);
        return View(items);
    }

    private List<SelectListItem> PopulateRepoGoToList(int userId, string currentRepositoryName)
    {
        var pullList = _repoPermissions.GetAllPermittedRepositories(userId, RepositoryAccessLevel.Pull);
        var adminList = _repoPermissions.GetAllPermittedRepositories(userId, RepositoryAccessLevel.Administer);
        var firstList = pullList.Union(adminList, InlineComparer<RepositoryModel>.Create((lhs, rhs) => lhs.Id == rhs.Id, obj => obj.Id.GetHashCode()))
            .OrderBy(x => x.Name.ToLowerInvariant())
            .GroupBy(x => x.Group ?? Resources.Repository_No_Group);

        var items = new List<SelectListItem>();
        var urlHelper = _urlHelperFactory.GetUrlHelper(ViewContext);

        items.Add(new SelectListItem { Text = Resources.Repository_Go_To_Dropdown, Value = "", Disabled = true, Selected = currentRepositoryName == null });
        items.Add(new SelectListItem { Text = Resources.Repository_Go_To_Overview, Value = urlHelper.Action("Index", "Repositories") });

        var groups = new Dictionary<string, SelectListGroup>();
        foreach (var grouped in firstList)
        {
            if (!groups.TryGetValue(grouped.Key, out var group))
            {
                group = new SelectListGroup { Name = grouped.Key };
                groups[grouped.Key] = group;
            }

            foreach (var item in grouped)
            {
                var selectListItem = new SelectListItem
                {
                    Text = item.Name,
                    Value = urlHelper.Action("Open", "Repositories", new { name = item.Name }),
                    Group = group,
                    Selected = item.Name == currentRepositoryName
                };

                items.Add(selectListItem);
            }
        }

        return items;
    }
}
