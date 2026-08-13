using Planorama.Core.Exceptions;
using Planorama.Core.Media;
using SkiaSharp;
using Xunit;

namespace Planorama.Tests.Unit;

public class SkiaImageProcessorTests
{
    private readonly SkiaImageProcessor _processor = new();

    [Fact]
    public void ProcessAvatar_crops_resizes_and_reencodes_a_valid_image()
    {
        using var source = GenerateSolidColorPng(width: 800, height: 600, SKColors.CornflowerBlue);

        var result = _processor.ProcessAvatar(source);

        Assert.Equal("image/jpeg", result.ContentType);
        using var decoded = SKBitmap.Decode(result.Bytes);
        Assert.Equal(512, decoded.Width);
        Assert.Equal(512, decoded.Height);
    }

    [Fact]
    public void ProcessAvatar_throws_for_corrupt_or_non_image_bytes()
    {
        using var garbage = new MemoryStream([1, 2, 3, 4, 5]);
        Assert.Throws<UnsupportedImageFormatException>(() => _processor.ProcessAvatar(garbage));
    }

    private static MemoryStream GenerateSolidColorPng(int width, int height, SKColor color)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(color);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return new MemoryStream(data.ToArray());
    }
}
