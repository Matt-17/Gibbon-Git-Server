using System.Threading.Tasks;

namespace Gibbon.Git.Server.Repositories;

public interface IRepositoryCopier
{
    Task<bool> CopyAsync(string sourcePath, string targetPath);
}
