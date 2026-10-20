using SkiaSharp;

namespace Gibbon.Git.Server.Services;

public interface IRepositoryLogoService
{
    byte[] CreateLogo(Stream imageStream);
}

public class RepositoryLogoService : IRepositoryLogoService
{
    private const int LargeLogoWidth = 72;
    private const int SmallLogoWidth = 36;
    private const long MaxSourcePixels = 50_000_000;

    public byte[] CreateLogo(Stream imageStream)
    {
        ArgumentNullException.ThrowIfNull(imageStream);

        using var codec = SKCodec.Create(imageStream);
        if (codec == null || (long)codec.Info.Width * codec.Info.Height > MaxSourcePixels)
        {
            return null;
        }

        using var source = SKBitmap.Decode(codec, codec.Info.WithColorType(SKColorType.Rgba8888).WithAlphaType(SKAlphaType.Premul));
        if (source == null)
        {
            return null;
        }

        var width = source.Width >= LargeLogoWidth ? LargeLogoWidth : SmallLogoWidth;
        var height = Math.Max(1, width * source.Height / source.Width);

        using var resized = source.Resize(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        if (resized == null)
        {
            return null;
        }

        using var image = SKImage.FromBitmap(resized);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data?.ToArray();
    }
}
