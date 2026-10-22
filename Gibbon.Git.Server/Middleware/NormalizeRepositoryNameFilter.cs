using System.Threading.Tasks;
using Gibbon.Git.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Gibbon.Git.Server.Middleware;

public class NormalizeRepositoryNameFilter(IRepositoryService repositoryService, IUrlHelperFactory urlHelperFactory)
    : IAsyncActionFilter
{
    private readonly IRepositoryService _repositoryService = repositoryService;
    private readonly IUrlHelperFactory _urlHelperFactory = urlHelperFactory;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.RouteData.Values.TryGetValue("name", out var nameObj))
        {
            await next();
            return;
        }

        var inputName = nameObj as string;
        if (!string.IsNullOrEmpty(inputName))
        {
            var name = _repositoryService.NormalizeRepositoryName(inputName);
            if (name != null && !string.Equals(name, inputName, StringComparison.Ordinal))
            {
                var routeValues = new RouteValueDictionary(context.RouteData.Values)
                {
                    ["name"] = name
                };

                var urlHelper = _urlHelperFactory.GetUrlHelper(context);

                var action = context.ActionDescriptor.RouteValues["action"];
                var controller = context.ActionDescriptor.RouteValues["controller"];

                var newUrl = urlHelper.Action(action, controller, routeValues, context.HttpContext.Request.Scheme);

                context.Result = new RedirectResult(newUrl, permanent: false);
                return;
            }
        }

        await next();
    }
}
