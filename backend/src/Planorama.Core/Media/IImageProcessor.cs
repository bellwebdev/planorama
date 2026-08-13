namespace Planorama.Core.Media;

public interface IImageProcessor
{
    /// <summary>Decodes, validates, center-crops to a square, resizes, and re-encodes as JPEG.</summary>
    /// <exception cref="Exceptions.UnsupportedImageFormatException">The stream doesn't decode as a supported image, or exceeds the source-dimension guard.</exception>
    ProcessedImage ProcessAvatar(Stream source);
}
