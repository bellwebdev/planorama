using Planorama.Core.Domain;

namespace Planorama.Core.Auth;

/// <summary>
/// Seam between Core and the JWT libraries: Core depends only on this interface, never on
/// System.IdentityModel.Tokens.Jwt directly. Implemented in Planorama.Api
/// (<c>JwtAccessTokenIssuer</c>), which is the layer that already owns <see cref="Planorama.Core.Options.JwtOptions"/>
/// and the JWT-bearer packages.
/// </summary>
public interface IAccessTokenIssuer
{
    AccessToken Issue(AppUser user);
}
