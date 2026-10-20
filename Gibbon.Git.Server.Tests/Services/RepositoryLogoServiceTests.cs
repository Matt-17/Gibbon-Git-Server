using System.IO;

using Gibbon.Git.Server.Services;

using SkiaSharp;

namespace Gibbon.Git.Server.Tests.Services;

[TestClass]
public class RepositoryLogoServiceTests
{
    private RepositoryLogoService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new RepositoryLogoService();
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    [DataRow(200, 100, 72, 36)]
    [DataRow(100, 400, 72, 288)]
    [DataRow(72, 72, 72, 72)]
    [DataRow(50, 50, 36, 36)]
    [DataRow(71, 142, 36, 72)]
    [DataRow(1000, 1, 72, 1)]
    public void CreateLogo_ResizesToLogoWidthKeepingAspectRatio(int sourceWidth, int sourceHeight, int expectedWidth, int expectedHeight)
    {
        using var input = CreateImage(sourceWidth, sourceHeight, SKEncodedImageFormat.Png);

        var logo = _service.CreateLogo(input);

        using var result = DecodePng(logo);
        Assert.AreEqual(expectedWidth, result.Width);
        Assert.AreEqual(expectedHeight, result.Height);
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    [DataRow(SKEncodedImageFormat.Jpeg)]
    [DataRow(SKEncodedImageFormat.Webp)]
    public void CreateLogo_AcceptsOtherFormatsAndReturnsPng(SKEncodedImageFormat format)
    {
        using var input = CreateImage(144, 72, format);

        var logo = _service.CreateLogo(input);

        using var result = DecodePng(logo);
        Assert.AreEqual(72, result.Width);
        Assert.AreEqual(36, result.Height);
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    public void CreateLogo_KeepsColors()
    {
        using var input = CreateImage(100, 100, SKEncodedImageFormat.Png);

        var logo = _service.CreateLogo(input);

        using var result = DecodePng(logo);
        var pixel = result.GetPixel(result.Width / 2, result.Height / 2);
        Assert.AreEqual(SKColors.Red, pixel);
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    public void CreateLogo_InvalidData_ReturnsNull()
    {
        using var input = new MemoryStream("this is not an image"u8.ToArray());

        Assert.IsNull(_service.CreateLogo(input));
    }

    [TestMethod]
    [TestCategory(TestCategories.UnitTest)]
    public void CreateLogo_EmptyStream_ReturnsNull()
    {
        using var input = new MemoryStream();

        Assert.IsNull(_service.CreateLogo(input));
    }

    private static MemoryStream CreateImage(int width, int height, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.Red);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 100);
        return new MemoryStream(data.ToArray());
    }

    private static SKBitmap DecodePng(byte[] logo)
    {
        Assert.IsNotNull(logo);
        using var codec = SKCodec.Create(new MemoryStream(logo));
        Assert.AreEqual(SKEncodedImageFormat.Png, codec.EncodedFormat);
        return SKBitmap.Decode(logo);
    }
}
