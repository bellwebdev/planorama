using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>403 — the external provider's token verified, but its email wasn't marked verified by the provider.</summary>
public class ExternalEmailNotVerifiedException()
    : AuthProblemException(HttpStatusCode.Forbidden, "Email not verified", "Your email address with this provider isn't verified.");
