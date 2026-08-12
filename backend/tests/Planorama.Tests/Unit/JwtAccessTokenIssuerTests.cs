using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Planorama.Api.Auth;
using Planorama.Core.Domain;
using Planorama.Core.Options;
using Xunit;

namespace Planorama.Tests.Unit;

public class JwtAccessTokenIssuerTests
{
    private static readonly JwtOptions Options = new()
    {
        Issuer = "planorama-tests",
        Audience = "planorama-tests",
        SigningKey = "unit-test-signing-key-at-least-32-characters",
        AccessTokenMinutes = 15,
    };

    [Fact]
    public void Issue_produces_a_token_with_the_expected_claims_and_expiry()
    {
        var issuer = new JwtAccessTokenIssuer(Microsoft.Extensions.Options.Options.Create(Options));
        var user = new AppUser { Id = Guid.NewGuid(), Email = "ada@example.com", DisplayName = "Ada" };

        var accessToken = issuer.Issue(user);

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(accessToken.Value, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Options.SigningKey)),
        }, out _);

        Assert.Equal(user.Id.ToString(), principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
        Assert.Equal(user.Email, principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value);
        Assert.Equal(user.DisplayName, principal.FindFirst("name")?.Value);
        Assert.NotNull(principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value);

        var expectedExpiry = DateTimeOffset.UtcNow.AddMinutes(Options.AccessTokenMinutes);
        Assert.True(Math.Abs((accessToken.ExpiresAt - expectedExpiry).TotalSeconds) < 5);
    }
}
