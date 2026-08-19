using Planorama.Api.Contracts.Trips;
using Planorama.Api.Validation;
using Planorama.Core.Domain;
using Xunit;

namespace Planorama.Tests.Unit;

public class CreateInviteRequestValidatorTests
{
    private readonly CreateInviteRequestValidator _validator = new();

    [Fact]
    public void Link_invite_without_contact_passes()
    {
        var result = _validator.Validate(new CreateInviteRequest(InvitedVia.Link, null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Email_invite_with_valid_contact_passes()
    {
        var result = _validator.Validate(new CreateInviteRequest(InvitedVia.Email, "friend@example.com"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Email_invite_without_contact_fails()
    {
        var result = _validator.Validate(new CreateInviteRequest(InvitedVia.Email, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Email_invite_with_invalid_contact_fails()
    {
        var result = _validator.Validate(new CreateInviteRequest(InvitedVia.Email, "not-an-email"));

        Assert.False(result.IsValid);
    }
}
