using Planorama.Api.Contracts.Auth;
using Planorama.Api.Validation;
using Xunit;

namespace Planorama.Tests.Unit;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        var result = _validator.Validate(new LoginRequest("ada@example.com", "anything"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Invalid_email_fails()
    {
        var result = _validator.Validate(new LoginRequest("not-an-email", "anything"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Empty_password_fails()
    {
        var result = _validator.Validate(new LoginRequest("ada@example.com", ""));

        Assert.False(result.IsValid);
    }
}
