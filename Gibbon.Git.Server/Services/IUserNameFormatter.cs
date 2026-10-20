using Gibbon.Git.Server.Models;

namespace Gibbon.Git.Server.Services;

public interface IUserNameFormatter
{
    string GetDisplayName(UserModel user);

    IEnumerable<UserModel> Sort(IEnumerable<UserModel> users);
}
