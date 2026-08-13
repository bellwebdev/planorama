using Planorama.Core.Exceptions;
using SkiaSharp;

namespace Planorama.Core.Media;

/// <inheritdoc cref="IImageProcessor"/>
public class SkiaImageProcessor : IImageProcessor
{
    private const int AvatarDimension = 512;
    private const int JpegQuality = 85;

    // Cheap decompression-bomb guard: reject sources whose pixel dimensions are absurd relative
    // to their (already size-capped) file size, before a full decode allocates memory for them.
    private const int MaxSourceDimension = 8000;

    public ProcessedImage ProcessAvatar(Stream source)
    {
        using var codec = SKCodec.Create(source) ?? throw new UnsupportedImageFormatException();
        if (codec.Info.Width > MaxSourceDimension || codec.Info.Height > MaxSourceDimension)
        {
            throw new UnsupportedImageFormatException();
        }

        using var original = SKBitmap.Decode(codec) ?? throw new UnsupportedImageFormatException();

        var cropSize = Math.Min(original.Width, original.Height);
        var cropRect = SKRectI.Create((original.Width - cropSize) / 2, (original.Height - cropSize) / 2, cropSize, cropSize);

        using var cropped = new SKBitmap(cropSize, cropSize);
        if (!original.ExtractSubset(cropped, cropRect))
        {
            throw new UnsupportedImageFormatException();
        }

        using var resized = cropped.Resize(new SKImageInfo(AvatarDimension, AvatarDimension), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
            ?? throw new UnsupportedImageFormatException();

        using var image = SKImage.FromBitmap(resized);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);

        return new ProcessedImage(data.ToArray(), "image/jpeg");
    }
}
