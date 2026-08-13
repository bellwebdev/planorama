using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>413 — the uploaded file exceeds the 5 MB avatar limit.</summary>
public class AvatarTooLargeException()
    : AuthProblemException(HttpStatusCode.RequestEntityTooLarge, "Avatar too large", "Avatar images must be 5 MB or smaller.");
