using Planorama.Api.Auth;
using Planorama.Core.Exceptions;

namespace Planorama.Tests.Integration;

/// <summary>
/// Test double standing in for real Google network verification. Tests encode the desired
/// payload directly in the "token" string as pipe-delimited fields (subject|email|verified|name),
/// or pass the sentinel "invalid" to simulate a token that fails verification.
/// </summary>
public class FakeGoogleIdTokenValidator : IGoogleIdTokenValidator
{
    public Task<GoogleIdTokenPayload> ValidateAsync(string idToken, CancellationToken ct)
    {
        if (idToken == "invalid")
        {
            throw new InvalidExternalTokenException();
        }

        var parts = idToken.Split('|');
        var subject = parts[0];
        var email = parts[1];
        var emailVerified = parts.Length < 3 || bool.Parse(parts[2]);
        var name = parts.Length > 3 && parts[3].Length > 0 ? parts[3] : null;

        return Task.FromResult(new GoogleIdTokenPayload(subject, email, emailVerified, name, null));
    }
}
