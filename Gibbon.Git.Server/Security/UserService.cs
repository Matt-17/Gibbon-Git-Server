using Gibbon.Git.Server.Data;
using Gibbon.Git.Server.Data.Entities;
using Gibbon.Git.Server.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Gibbon.Git.Server.Security;

public class UserService(ILogger<UserService> logger, IMemoryCache memoryCache, GibbonGitServerContext context, IPasswordService passwordService)
    : IUserService
{
    private readonly ILogger<UserService> _logger = logger;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly GibbonGitServerContext _context = context;
    private readonly IPasswordService _passwordService = passwordService;

    public bool IsPasswordValid(string username, string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(username, nameof(username));
        ArgumentException.ThrowIfNullOrEmpty(password, nameof(password));

        _logger.LogDebug("Validating user {UserName}", username);

        username = username.ToLowerInvariant();
        var user = _context.Users.FirstOrDefault(i => i.Username == username);
        if (user == null)
        {
            _logger.LogWarning("Failed to find user {UserName}", username);
            return false;
        }

        var result = _passwordService.CompareHash(user.PasswordSalt, password, user.Password);
        _logger.LogDebug("User {UserName} validation result {Result}", username, result);
        return result;
    }

    public bool CreateUser(string username, string password, string givenName, string surname, string email)
    {
        ArgumentException.ThrowIfNullOrEmpty(username, nameof(username));
        ArgumentException.ThrowIfNullOrEmpty(password, nameof(password));
        ArgumentException.ThrowIfNullOrEmpty(givenName, nameof(givenName));
        ArgumentException.ThrowIfNullOrEmpty(surname, nameof(surname));
        ArgumentException.ThrowIfNullOrEmpty(email, nameof(email));

        var user = new User
        {
            Username = username.ToLowerInvariant(),
            GivenName = givenName,
            Surname = surname,
            Email = email,
        };
        SetPassword(user, password);
        _context.Users.Add(user);
        try
        {
            _context.SaveChanges();
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    public List<UserModel> GetAllUsers()
    {
        return _context.Users
            .AsNoTracking()
            .Select(item => new UserModel
            {
                Id = item.Id,
                Username = item.Username,
                GivenName = item.GivenName,
                Surname = item.Surname,
                Email = item.Email,
            })
            .AsEnumerable()
            .OrderBy(x => x.SortName)
            .ToList();
    }

    public int UserCount()
    {
        return _context.Users.Count();
    }

    public UserModel GetUserModel(int id)
    {
        if (_memoryCache.TryGetValue(GetUserKey(id), out UserModel user))
        {
            return user;
        }

        user = _context.Users
           .Where(u => u.Id == id)
           .Select(u => new UserModel
           {
               Id = u.Id,
               Username = u.Username,
               GivenName = u.GivenName,
               Surname = u.Surname,
               Email = u.Email
           })
           .FirstOrDefault();

        CacheUser(user);
      
        return user;
    }

    public UserModel GetUserModel(string username)
    {
        if (_memoryCache.TryGetValue(GetUserKey(username), out UserModel user))
        {
            return user;
        }

        user = _context.Users
           .Where(u => u.Username == username)
           .Select(u => new UserModel
           {
               Id = u.Id,
               Username = u.Username,
               GivenName = u.GivenName,
               Surname = u.Surname,
               Email = u.Email
           })
           .FirstOrDefault();

        CacheUser(user);
       
        return user;
    }

    private void CacheUser(UserModel user)
    {
        if (user == null)
        {
            return;
        }

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };

        _memoryCache.Set(GetUserKey(user.Id), user, cacheOptions);
        _memoryCache.Set(GetUserKey(user.Username), user, cacheOptions);
    }

    public void DeleteUser(int id)
    {
        var user = _context.Users.FirstOrDefault(u => u.Id == id);
        if (user != null)
        {
            user.Roles.Clear();
            user.Repositories.Clear();
            user.Teams.Clear();
            _context.Users.Remove(user);
            _context.SaveChanges();
        }
    }

    public string GenerateResetToken(string username)
    {
        return _passwordService.GenerateToken(username);
    }

    public void UpdateUser(int id, string givenName, string surname, string email)
    {
        var user = _context.Users.FirstOrDefault(i => i.Id == id);
        if (user == null)
            return;

        user.GivenName = givenName ?? user.GivenName;
        user.Surname = surname ?? user.Surname;
        user.Email = email ?? user.Email;
        _context.SaveChanges();

        _memoryCache.Remove(GetUserKey(user.Id));
        _memoryCache.Remove(GetUserKey(user.Username));
    }

    public void UpdatePassword(int id, string newPassword)
    {
        var user = _context.Users.FirstOrDefault(i => i.Id == id);
        if (user == null)
        {
            throw new InvalidOperationException("UserId not valid");
        }

        SetPassword(user, newPassword);
        _context.SaveChanges();
    }

    private static string GetUserKey(int id) => $"User_Id_{id}";
    private static string GetUserKey(string username) => $"User_Username_{username.ToLowerInvariant()}";

    private void SetPassword(User user, string password)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));
        ArgumentException.ThrowIfNullOrEmpty(password, nameof(password));

        user.PasswordSalt = _passwordService.GenerateSalt();
        user.Password = _passwordService.GenerateHash(user.PasswordSalt, password);
    }
}
