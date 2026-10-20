using System.ComponentModel.DataAnnotations;

namespace Gibbon.Git.Server.Models;

public class RepositoryLogoDetailModel
{
    public RepositoryLogoDetailModel() { }

    public RepositoryLogoDetailModel(byte[] data)
    {
        BinaryData = data;
    }

    [Display(ResourceType = typeof(Resources), Name = "Repository_Detail_Logo_PostedFile")]
    public IFormFile PostedFile { get; set; }

    [Display(ResourceType = typeof(Resources), Name = "Repository_Detail_RemoveLogo")]
    public bool RemoveLogo { get; set; }

    public byte[] BinaryData { get; set; }

    public bool Exists => BinaryData != null;

    public string Base64Image => BinaryData != null ? Convert.ToBase64String(BinaryData) : null;
}
