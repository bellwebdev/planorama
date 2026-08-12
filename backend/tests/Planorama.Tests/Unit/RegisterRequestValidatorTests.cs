using Planorama.Api.Contracts.Auth;
using Planorama.Api.Validation;
using Xunit;

namespace Planorama.Tests.Unit;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        var result = _validator.Validate(new RegisterRequest("ada@example.com", "Passw0rd!23", "Ada"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("short1!")] // too short
    [InlineData("nouppercase1!")] // no uppercase
    [InlineData("NOLOWERCASE1!")] // no lowercase
    [InlineData("NoDigitsHere!")] // no digit
    [InlineData("NoSpecialChar123")] // no non-alphanumeric
    public void Weak_password_fails(string password)
    {
        var result = _validator.Validate(new RegisterRequest("ada@example.com", password, "Ada"));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Invalid_email_fails(string email)
    {
        var result = _validator.Validate(new RegisterRequest(email, "Passw0rd!23", "Ada"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Empty_display_name_fails()
    {
        var result = _validator.Validate(new RegisterRequest("ada@example.com", "Passw0rd!23", ""));

        Assert.False(result.IsValid);
    }
}
