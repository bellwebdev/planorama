namespace Planorama.Core.Auth;

/// <summary>Provider-agnostic identity claims from a verified external sign-in token. The verification itself (signature/issuer/audience) happens in Planorama.Api, upstream of this record.</summary>
public record ExternalLoginIdentity(string Provider, string ProviderKey, string Email, bool EmailVerified, string? DisplayName, string? AvatarUrl);
