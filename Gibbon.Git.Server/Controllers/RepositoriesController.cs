using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Gibbon.Git.Server.Configuration;
using Gibbon.Git.Server.Data;
using Gibbon.Git.Server.Extensions;
using Gibbon.Git.Server.Helpers;
using Gibbon.Git.Server.Middleware;
using Gibbon.Git.Server.Models;
using Gibbon.Git.Server.Repositories;
using Gibbon.Git.Server.Security;
using Gibbon.Git.Server.Services;

using ICSharpCode.SharpZipLib.Zip;

using LibGit2Sharp;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gibbon.Git.Server.Controllers;

[Authorize]
[TypeFilter(typeof(NormalizeRepositoryNameFilter))]
public class RepositoriesController(ILogger<RepositoriesController> logger, ITeamService teamRepository, IRepositoryService repositoryService, IUserService userService, IRepositoryPermissionService repositoryPermissionService, IRepositorySynchronizer repositorySynchronizer, ServerSettings serverSettings, IPathResolver pathResolver, IRepositoryBrowserFactory repositoryBrowserFactory, IRepositoryLogoService repositoryLogoService)
    : Controller
{
    private readonly ServerSettings _serverSettings = serverSettings;
    private readonly IPathResolver _pathResolver = pathResolver;
    private readonly ILogger<RepositoriesController> _logger = logger;
    private readonly ITeamService _teamRepository = teamRepository;
    private readonly IRepositoryService _repositoryService = repositoryService;
    private readonly IUserService _userService = userService;
    private readonly IRepositoryPermissionService _repositoryPermissionService = repositoryPermissionService;
    private readonly IRepositorySynchronizer _repositorySynchronizer = repositorySynchronizer;
    private readonly IRepositoryBrowserFactory _repositoryBrowserFactory = repositoryBrowserFactory;
    private readonly IRepositoryLogoService _repositoryLogoService = repositoryLogoService;
    private const int _pageSize = 10;

    public IActionResult Index(string sortGroup = null, string searchString = null)
    {
        var firstList = _repositoryPermissionService
            .GetAllPermittedRepositories(User.Id(), RepositoryAccessLevel.Pull)
            .Select(x => ConvertRepositoryModel(x, User))
            .ToList();
        if (!string.IsNullOrEmpty(searchString))
        {
            var search = searchString.ToLower();
            firstList = firstList.Where(a => a.Name.ToLower().Contains(search) || (!string.IsNullOrEmpty(a.Group) && a.Group.ToLower().Contains(search)) || (!string.IsNullOrEmpty(a.Description) && a.Description.ToLower().Contains(search)))
                .ToList();
        }

        foreach (var item in firstList)
        {
            SetGitUrls(item);
        }
        var list = firstList
            .OrderBy(x => x.Name)
            .GroupBy(x => x.Group)
            .OrderBy(x => x.Key, string.IsNullOrEmpty(sortGroup) || sortGroup.Equals("ASC"))
            .ToDictionary(x => x.Key ?? string.Empty, x => x.ToArray());

        return View(list);
    }

    [HttpGet("Repositories/{name}")]
    [Authorize(Policy = Policies.RepositoryPull)]
    public IActionResult Detail(string name)
    {
        var repository = _repositoryService.GetRepository(name);
        ViewBag.Name = name;

        var model = ConvertRepositoryModel(repository, User);
        if (model == null)
        {
            return View(model);
        }

        model.IsCurrentUserAdministrator = _repositoryPermissionService.HasPermission(User.Id(), model.Id, RepositoryAccessLevel.Administer);
        SetGitUrls(model);

        using (var browser = _repositoryBrowserFactory.Create(model.Name))
        {
            browser.BrowseTree(null, null, out var defaultReferenceName);
            RouteData.Values.Add("version", defaultReferenceName);
        }

        return View(model);
    }

    [HttpGet("Repositories/{name}/Edit")]
    [Authorize(Policy = Policies.RepositoryAdmin)]
    public IActionResult Edit(string name)
    {
        var model = ConvertRepositoryModel(_repositoryService.GetRepository(name), User);
        PopulateCheckboxListData(ref model);
        return View(model);
    }

    [HttpPost("Repositories/{name}/Edit")]
    [Authorize(Policy = Policies.RepositoryAdmin)]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(RepositoryDetailModel model)
    {
        ProcessPostedLogo(model);
        if (!ModelState.IsValid)
        {
            PopulateCheckboxListData(ref model);
            return View(model);
        }

        var currentUserIsInAdminList = model.PostedSelectedAdministrators != null && model.PostedSelectedAdministrators.Contains(User.Id());
        if (currentUserIsInAdminList || User.IsInRole(Roles.Admin))
        {
            var repoModel = ConvertRepositoryDetailModel(model);
            try
            {
                _repositoryService.Update(repoModel);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to update repo {RepoName}", model.Name);
                ModelState.AddModelError("Administrators", Resources.Repository_Edit_CantRemoveYourself);
                PopulateCheckboxListData(ref model);
                return View(model);
            }

            ViewBag.UpdateSuccess = true;
        }
        else
        {
            ModelState.AddModelError("Administrators", Resources.Repository_Edit_CantRemoveYourself);
            PopulateCheckboxListData(ref model);
            return View(model);
        }

        PopulateCheckboxListData(ref model);
        return RedirectToAction("Edit", new { model.Id });
    }

    [HttpGet("Repositories/Create")]
    public IActionResult Create()
    {
        if (!_repositoryPermissionService.HasCreatePermission(User.Id()))
        {
            return Unauthorized();
        }

        var model = new RepositoryDetailModel
        {
            Administrators = [_userService.GetUserModel(User.Id())],
        };
        PopulateCheckboxListData(ref model);
        return View(model);
    }

    [HttpPost("Repositories/Create")]
    [ValidateAntiForgeryToken]
    public IActionResult Create(RepositoryDetailModel model)
    {
        ArgumentNullException.ThrowIfNull(model, nameof(model));
        _logger.LogInformation("Create repository {0}", model.Name);

        if (!_repositoryPermissionService.HasCreatePermission(User.Id()))
        {
            return Unauthorized();
        }

        PopulateCheckboxListData(ref model);
        ProcessPostedLogo(model);
        model.Name = StringHelper.RemoveWhiteSpace(model.Name);

        if (!_repositoryService.NameIsUnique(model.Name, model.Id))
        {
            ModelState.AddModelError("Name", Resources.Validation_Duplicate_Name);
            return View(model);
        }

        if (string.IsNullOrEmpty(model.Name))
        {
            ModelState.AddModelError("Name", Resources.Repository_Create_NameFailure);
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var path = Path.Combine(_pathResolver.GetRepositories(), model.Name);
        if (Directory.Exists(path))
        {
            _repositoryService.Delete(model.Id);
            ModelState.AddModelError("", Resources.Repository_Create_DirectoryExists);
            return View(model);
        }

        var repoModel = ConvertRepositoryDetailModel(model);
        if (!_repositoryService.Create(repoModel))
        {
            ModelState.AddModelError("", Resources.Repository_Create_Failure);
            return View(model);
        }

        Repository.Init(path, true);
        TempData["CreateSuccess"] = true;
        TempData["SuccessfullyCreatedRepositoryName"] = model.Name;
        return RedirectToAction("Index");

    }

    [HttpGet("Repositories/{name}/Delete")]
    [Authorize(Policy = Policies.RepositoryAdmin)]
    public IActionResult Delete(string name)
    {
        return View(ConvertRepositoryModel(_repositoryService.GetRepository(name), User));
    }

    [HttpPost("Repositories/{name}/Delete")]
    [Authorize(Policy = Policies.RepositoryAdmin)]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(RepositoryDetailModel model)
    {
        if (model != null)
        {
            var repo = _repositoryService.GetRepository(model.Id);
            var path = Path.Combine(_pathResolver.GetRepositories(), repo.Name);
            if (Directory.Exists(path))
            {
                DeleteFileSystemInfo(new DirectoryInfo(path));
            }
            _repositoryService.Delete(repo.Id);
            TempData["DeleteSuccess"] = true;
        }
        return RedirectToAction("Index");
    }

    [HttpGet("Repositories/{name}/Tree/{**path}")]
    [Authorize(Policy = Policies.RepositoryPush)]
    public IActionResult Tree(string name, string version, string path)
    {
        var isApiRequest = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        ViewBag.Name = name;

        var repo = _repositoryService.GetRepository(name);

        using var browser = _repositoryBrowserFactory.Create(repo.Name);
        var files = browser.BrowseTree(version, path, out var referenceName, true).ToList();

        var readme = files.FirstOrDefault(x => x.Name.Equals("readme.md", StringComparison.OrdinalIgnoreCase));
        var readmeTxt = string.Empty;
        if (readme != null)
        {
            var blob = browser.BrowseBlob(version, readme.Path, out _);
            readmeTxt = blob.Text;
        }
        var model = new RepositoryTreeModel
        {
            Name = repo.Name,
            Branch = version ?? referenceName,
            Path = path,
            Readme = readmeTxt,
            Logo = new RepositoryLogoDetailModel(repo.Logo),
            Files = files.OrderByDescending(i => i.IsTree).ThenBy(i => i.Name)
        };

        if (isApiRequest)
        {
            return Json(model);
        }

        PopulateBranchesData(browser, version ?? referenceName);
        PopulateAddressBarData(path);
        return View(model);
    }

    [HttpGet("Repositories/{name}/Blob/{**path}")]
    [Authorize(Policy = Policies.RepositoryPush)]
    public IActionResult Blob(string name, string version, string path)
    {
        ViewBag.Name = name;
        var repo = _repositoryService.GetRepository(name);
        using var browser = _repositoryBrowserFactory.Create(repo.Name);
        var model = browser.BrowseBlob(version, path, out var referenceName);
        model.Logo = new RepositoryLogoDetailModel(repo.Logo);
        PopulateBranchesData(browser, referenceName ?? version);
        PopulateAddressBarData(path);

        return View(model);
    }

    [HttpGet("Repositories/{name}/Raw/{**path}")]
    [Authorize(Policy = Policies.RepositoryPush)]
    public IActionResult Raw(string name, string version, string path, bool display = false)
    {
        var repo = _repositoryService.GetRepository(name);
        using var browser = _repositoryBrowserFactory.Create(repo.Name);
        var model = browser.BrowseBlob(version, path, out _);

        if (model.IsText && display)
        {
            return Content(model.Text, "text/plain", model.Encoding);
        }

        if (model.IsText && !display)
        {
            var textData = System.Text.Encoding.UTF8.GetBytes(model.Text);
            return File(textData, "application/octet-stream", model.Name);
        }

        if (model.IsImage && display)
        {
            Response.Headers.ContentDisposition = "inline";
            return File(model.Data, MimeTypes.GetMimeType(model.Name));
        }

        if (model.IsImage && !display)
        {
            return File(model.Data, "application/octet-stream", model.Name);
        }

        return NotFound();
    }

    [HttpGet("Repositories/{name}/Blame/{**path}")]
    [Authorize(Policy = Policies.RepositoryPush)]
    public IActionResult Blame(string name, string version, string path)
    {
        ViewBag.Name = name;
        ViewBag.ShowShortMessageOnly = true;
        var repo = _repositoryService.GetRepository(name);
        using var browser = _repositoryBrowserFactory.Create(repo.Name);
        var model = browser.GetBlame(version, path, out var referenceName);
        model.Logo = new RepositoryLogoDetailModel(repo.Logo);
        PopulateBranchesData(browser, referenceName);
        PopulateAddressBarData(path);

        return View(model);
    }

    [HttpGet("Repositories/{name}/Download/{**path}")]
    [Authorize(Policy = Policies.RepositoryPush)]
    public async Task<IActionResult> Download(string name, string version, string path)
    {
        var repo = _repositoryService.GetRepository(name);

        Response.ContentType = "application/zip";
        Response.Headers["Content-Disposition"] = $"attachment; filename={repo.Name}.zip";

        await using var zipStream = new ZipOutputStream(Response.Body);
        zipStream.IsStreamOwner = false;
        zipStream.UseZip64 = UseZip64.On;
        zipStream.SetLevel(9);

        using var browser = _repositoryBrowserFactory.Create(repo.Name);
        try
        {
            await AddTreeToZip(browser, version, path, zipStream, HttpContext.RequestAborted);
            await zipStream.FinishAsync(HttpContext.RequestAborted);
            return Empty;
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating zip file");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the zip file.");
        }
    }

    [HttpGet("Repositories/{name}/Tags/{branch?}")]
    [Authorize(Policy = Policies.RepositoryPush)]
    public IActionResult Tags(string name, string branch, int page = 1)
    {
        page = page >= 1 ? page : 1;

        ViewBag.Name = name;
        ViewBag.ShowShortMessageOnly = true;
        var repo = _repositoryService.GetRepository(name);
        using var browser = _repositoryBrowserFactory.Create(repo.Name);
        var commits = browser.GetTags(branch, page, 10, out var referenceName, out var totalCount);
        PopulateBranchesData(browser, referenceName);
        ViewBag.TotalCount = totalCount;
        return View(new RepositoryCommitsModel
        {
            Commits = commits,
            Name = repo.Name,
            Logo = new RepositoryLogoDetailModel(repo.Logo)
        });
    }

    [HttpGet("Repositories/{name}/Commits/")]
    [Authorize(Policy = Policies.RepositoryPush)]
    public IActionResult Commits(string name, [FromQuery] string branch = "master", [FromQuery] int page = 1)
    {
        page = page >= 1 ? page : 1;
        ViewBag.Page = page;

        ViewBag.ShowShortMessageOnly = true;
        var repo = _repositoryService.GetRepository(name);
        using var browser = _repositoryBrowserFactory.Create(repo.Name);
        var commits = browser.GetCommits(branch, page, _pageSize, out var referenceName, out var totalCount);
        PopulateBranchesData(browser, referenceName);
        ViewBag.TotalCount = totalCount;

        var linksreg = repo.LinksUseGlobal ? _serverSettings.LinksRegex : repo.LinksRegex;
        var linksurl = repo.LinksUseGlobal ? _serverSettings.LinksUrl : repo.LinksUrl;

        foreach (var commit in commits)
        {
            var links = new List<string>();
            if (!string.IsNullOrEmpty(linksreg))
            {
                try
                {
                    var matches = Regex.Matches(commit.Message, linksreg);
                    if (matches.Count > 0)
                    {
                        foreach (Match match in matches)
                        {
                            var groups = match.Groups.Cast<Group>();
                            var link = "";
                            try
                            {
                                var m = groups.Select(x => x.ToString()).ToArray();
                                link = string.Format(linksurl, m);
                            }
                            catch (FormatException e)
                            {
                                link = "An error occured while trying to format the link. Exception: " + e.Message;
                            }
                            links.Add(link);
                        }
                    }
                }
                catch (ArgumentException e)
                {
                    links.Add("An error occured while trying to match the regualar expression. Error: " + e.Message);
                }
            }
            commit.Links = links;
        }

        return View(new RepositoryCommitsModel
        {
            Commits = commits,
            Name = repo.Name,
            Logo = new RepositoryLogoDetailModel(repo.Logo),
            Pagination = new PaginationModel()
            {
                Branch = branch,
                PageInfo = new()
                {
                    CurrentPage = page,
                    ItemsPerPage = _pageSize,
                    TotalItems = totalCount
                }
            }
        });
    }

    [HttpGet("Repositories/{name}/Commit/{commit}")]
    [Authorize(Policy = Policies.RepositoryPush)]
    public IActionResult Commit(string name, string commit)
    {
        ViewBag.Name = name;
        ViewBag.ShowShortMessageOnly = false;
        var repo = _repositoryService.GetRepository(name);
        using var browser = _repositoryBrowserFactory.Create(repo.Name);
        var model = browser.GetCommitDetail(commit);
        model.Name = repo.Name;
        model.Logo = new RepositoryLogoDetailModel(repo.Logo);
        return View(model);
    }

    [HttpGet("Repositories/{name}/Clone")]
    public IActionResult Clone(string name)
    {
        if (!_repositoryPermissionService.HasCreatePermission(User.Id()))
        {
            return Unauthorized();
        }

        var model = ConvertRepositoryModel(_repositoryService.GetRepository(name), User);
        model.Name = "";
        PopulateCheckboxListData(ref model);
        ViewBag.Name = name;
        return View(model);
    }

    [HttpPost("Repositories/{name}/Clone")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RepositoryPush)]
    public IActionResult Clone(string name, RepositoryDetailModel model)
    {
        ArgumentNullException.ThrowIfNull(model, nameof(model));

        if (!_repositoryPermissionService.HasCreatePermission(User.Id()))
        {
            return Unauthorized();
        }

        ViewBag.Name = name;
        PopulateCheckboxListData(ref model);
        ProcessPostedLogo(model);
        model.Name = StringHelper.RemoveWhiteSpace(model.Name);

        if (string.IsNullOrEmpty(model.Name))
        {
            ModelState.AddModelError("Name", Resources.Repository_Create_NameFailure);
            return View(model);
        }

        if (!_repositoryService.NameIsUnique(model.Name, 0))
        {
            ModelState.AddModelError("Name", Resources.Validation_Duplicate_Name);
            return View(model);
        }

        var targetRepositoryPath = Path.Combine(_pathResolver.GetRepositories(), model.Name);
        if (Directory.Exists(targetRepositoryPath))
        {
            ModelState.AddModelError("", Resources.Repository_Create_DirectoryExists);
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var repoModel = ConvertRepositoryDetailModel(model);
        if (!_repositoryService.Create(repoModel))
        {
            ModelState.AddModelError("", Resources.Repository_Create_Failure);
            return View(model);
        }

        var sourceRepo = _repositoryService.GetRepository(name);
        var sourceRepositoryPath = Path.Combine(_pathResolver.GetRepositories(), sourceRepo.Name);

        var options = new CloneOptions
        {
            IsBare = true,
            Checkout = false
        };

        Repository.Clone(sourceRepositoryPath, targetRepositoryPath, options);

        using (var repo = new Repository(targetRepositoryPath))
        {
            if (repo.Network.Remotes.Any(r => r.Name == "origin"))
            {
                repo.Network.Remotes.Remove("origin");
            }
        }

        TempData["CloneSuccess"] = true;
        return RedirectToAction("Index");
    }

    [HttpGet("Repositories/{name}/History/{**path}")]
    [Authorize(Policy = Policies.RepositoryPush)]
    public IActionResult History(string name, string version, string path)
    {
        ViewBag.Name = name;
        ViewBag.ShowShortMessageOnly = true;
        var repo = _repositoryService.GetRepository(name);
        using var browser = _repositoryBrowserFactory.Create(repo.Name);
        var commits = browser.GetHistory(path, version, out _);
        return View(new RepositoryCommitsModel
        {
            Commits = commits,
            Name = repo.Name,
            Logo = new RepositoryLogoDetailModel(repo.Logo)
        });
    }

    [HttpPost("Repositories/Rescan")]
    [Authorize(Roles = Roles.Admin)]
    public IActionResult Rescan()
    {
        _repositorySynchronizer.SynchronizeRepository();
        return RedirectToAction("Index");
    }

    private static async Task AddTreeToZip(IRepositoryBrowser browser, string name, string path, ZipOutputStream zipStream, CancellationToken cancellationToken)
    {
        var treeNode = browser.BrowseTree(name, path, out _);

        foreach (var item in treeNode)
        {
            if (item.IsLink)
            {
                var entryName = Path.Combine(item.TreeName, item.Path).Replace("\\", "/"); // Für Konsistenz in Zip-Pfaden
                var entry = new ZipEntry(entryName + "/")
                {
                    DateTime = DateTime.Now,
                    Size = 0
                };
                await zipStream.PutNextEntryAsync(entry, cancellationToken);
                await zipStream.CloseEntryAsync(cancellationToken);
            }
            else if (!item.IsTree)
            {
                var model = browser.BrowseBlob(item.TreeName, item.Path, out _);
                var entryName = Path.Combine(item.TreeName, item.Path).Replace("\\", "/");

                var entry = new ZipEntry(entryName)
                {
                    DateTime = DateTime.Now,
                    Size = model.Data.Length
                };

                await zipStream.PutNextEntryAsync(entry, cancellationToken);
                await zipStream.WriteAsync(model.Data, 0, model.Data.Length, cancellationToken);
                await zipStream.CloseEntryAsync(cancellationToken);
            }
            else
            {
                await AddTreeToZip(browser, item.TreeName, item.Path, zipStream, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Construct the URLs for the repository
    /// </summary>
    private void SetGitUrls(RepositoryDetailModel model)
    {
        var request = HttpContext.Request;

        // Use IConfiguration to read the GitServerPath from the appsettings
        string serverAddress = null;// _configuration["GitServerPath"];

        // If GitServerPath is not defined, build it dynamically
        if (string.IsNullOrEmpty(serverAddress))
        {
            var port = request.Host.Port.HasValue && request.Host.Port != 80 && request.Host.Port != 443
                ? $":{request.Host.Port.Value}"
                : string.Empty;

            serverAddress = $"{request.Scheme}://{request.Host.Host}{port}{request.PathBase}/";
        }

        // Set the public Git URL for the repository
        model.GitUrl = string.Concat(serverAddress, model.Name, ".git");

        // Set the personal Git URL if the user is authenticated
        if (User.Identity.IsAuthenticated)
        {
            var username = Uri.EscapeDataString(User.Identity.Name);
            model.PersonalGitUrl = string.Concat(serverAddress.Replace("://", "://" + username + "@"), model.Name, ".git");
        }
    }

    private void PopulateAddressBarData(string path)
    {
        ViewData["path"] = path;
    }

    private void PopulateBranchesData(IRepositoryBrowser browser, string referenceName)
    {
        ViewData["referenceName"] = referenceName;
        ViewData["branches"] = browser.GetBranches();
        ViewData["tags"] = browser.GetTags();
    }

    public RepositoryDetailModel ConvertRepositoryModel(RepositoryModel model, IPrincipal user)
    {
        return model == null ? null : new RepositoryDetailModel
        {
            Id = model.Id,
            Name = model.Name,
            Group = model.Group,
            Description = model.Description,
            Users = model.Users,
            Administrators = model.Administrators,
            Teams = model.Teams,
            IsCurrentUserAdministrator = model.Administrators.Select(x => x.Id).Contains(user.Id()),
            AllowAnonymous = model.AnonymousAccess,
            AllowAnonymousPush = model.AllowAnonymousPush,
            Status = GetRepositoryStatus(model),
            Logo = new RepositoryLogoDetailModel(model.Logo),
            LinksUseGlobal = model.LinksUseGlobal,
            LinksRegex = model.LinksRegex,
            LinksUrl = model.LinksUrl,
        };
    }

    private void PopulateCheckboxListData(ref RepositoryDetailModel model)
    {
        model = model.Id != 0 ? ConvertRepositoryModel(_repositoryService.GetRepository(model.Id), User) : model;
        model.AllAdministrators = _userService.GetAllUsers().ToArray();
        model.AllUsers = _userService.GetAllUsers().ToArray();
        model.AllTeams = _teamRepository.GetAllTeams().ToArray();
        if (model.PostedSelectedUsers != null && model.PostedSelectedUsers.Any())
        {
            model.Users = model.PostedSelectedUsers.Select(x => _userService.GetUserModel(x)).ToArray();
        }
        if (model.PostedSelectedTeams != null && model.PostedSelectedTeams.Any())
        {
            model.Teams = model.PostedSelectedTeams.Select(x => _teamRepository.GetTeam(x)).ToArray();
        }
        if (model.PostedSelectedAdministrators != null && model.PostedSelectedAdministrators.Any())
        {
            model.Administrators = model.PostedSelectedAdministrators.Select(x => _userService.GetUserModel(x)).ToArray();
        }
    }

    private RepositoryDetailStatus GetRepositoryStatus(RepositoryModel model)
    {
        var repositoryPath = _pathResolver.GetRepositoryPath(model.Name);
        if (!Directory.Exists(repositoryPath))
        {
            return RepositoryDetailStatus.Missing;
        }

        if (!Repository.IsValid(repositoryPath))
        {
            return RepositoryDetailStatus.Invalid;
        }

        return RepositoryDetailStatus.Valid;
    }

    private void ProcessPostedLogo(RepositoryDetailModel model)
    {
        var postedFile = model.Logo?.PostedFile;
        if (postedFile == null)
        {
            return;
        }

        using (var stream = postedFile.OpenReadStream())
        {
            model.Logo.BinaryData = _repositoryLogoService.CreateLogo(stream);
        }

        if (model.Logo.BinaryData == null)
        {
            ModelState.AddModelError("Logo.PostedFile", string.Format(Resources.FileUploadAttribute_ErrorMessage, Resources.Repository_Detail_Logo_PostedFile, "png, jpg, jpeg, gif"));
        }
    }

    private RepositoryModel ConvertRepositoryDetailModel(RepositoryDetailModel model)
    {
        return model == null ? null : new RepositoryModel
        {
            Id = model.Id,
            Name = model.Name,
            Group = model.Group,
            Description = model.Description,
            Users = model.PostedSelectedUsers != null ? model.PostedSelectedUsers.Select(x => _userService.GetUserModel(x)).ToArray() : [],
            Administrators = model.PostedSelectedAdministrators != null ? model.PostedSelectedAdministrators.Select(x => _userService.GetUserModel(x)).ToArray() : [],
            Teams = model.PostedSelectedTeams != null ? model.PostedSelectedTeams.Select(x => _teamRepository.GetTeam(x)).ToArray() : [],
            AnonymousAccess = model.AllowAnonymous,
            Logo = model.Logo?.BinaryData,
            AllowAnonymousPush = model.AllowAnonymousPush,
            RemoveLogo = model.Logo is { RemoveLogo: true },
            LinksUseGlobal = model.LinksUseGlobal,
            LinksRegex = model.LinksRegex ?? "",
            LinksUrl = model.LinksUrl ?? ""
        };
    }

    private static void DeleteFileSystemInfo(FileSystemInfo fileSystemInfo)
    {
        fileSystemInfo.Attributes = FileAttributes.Normal;

        if (fileSystemInfo is DirectoryInfo directoryInfo)
        {
            foreach (var dirInfo in directoryInfo.GetFileSystemInfos())
            {
                DeleteFileSystemInfo(dirInfo);
            }
        }

        fileSystemInfo.Delete();
    }

    // TODO In a different Action we want to rename a repository and MoveRepo should be called there
    private void MoveRepo(RepositoryModel oldRepo, RepositoryModel newRepo)
    {
        if (oldRepo.Name != newRepo.Name)
        {
            var oldPath = Path.Combine(_pathResolver.GetRepositories(), oldRepo.Name);
            var newPath = Path.Combine(_pathResolver.GetRepositories(), newRepo.Name);
            try
            {
                Directory.Move(oldPath, newPath);
            }
            catch (IOException exc)
            {
                ModelState.AddModelError("Name", exc.Message);
            }
        }
    }
}
