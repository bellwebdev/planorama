using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>400 — the uploaded file doesn't decode as a supported image (corrupt or unsupported format).</summary>
public class UnsupportedImageFormatException()
    : AuthProblemException(HttpStatusCode.BadRequest, "Unsupported image format", "The uploaded file isn't a valid or supported image.");
