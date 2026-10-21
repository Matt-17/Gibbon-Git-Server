using Gibbon.Git.Server.Models;

namespace Gibbon.Git.Server.Data.Entities;

public class User
{
    public int Id { get; set; }
    public string GivenName { get; set; }
    public string Surname { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string PasswordSalt { get; set; }
    public string Email { get; set; }

    public virtual ICollection<Repository> Repositories { get; set; } = new HashSet<Repository>();

    public virtual ICollection<Role> Roles { get; set; } = new HashSet<Role>();

    public virtual ICollection<Team> Teams { get; set; } = new HashSet<Team>();

    public UserModel ToModel() => new()
    {
        Id = Id,
        Username = Username,
        GivenName = GivenName,
        Surname = Surname,
        Email = Email,
    };
}
