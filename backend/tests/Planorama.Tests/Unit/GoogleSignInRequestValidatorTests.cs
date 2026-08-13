using Planorama.Api.Contracts.Auth;
using Planorama.Api.Validation;
using Xunit;

namespace Planorama.Tests.Unit;

public class GoogleSignInRequestValidatorTests
{
    private readonly GoogleSignInRequestValidator _validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        var result = _validator.Validate(new GoogleSignInRequest("some-jwt-looking-token"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Empty_id_token_fails()
    {
        var result = _validator.Validate(new GoogleSignInRequest(""));

        Assert.False(result.IsValid);
    }
}
