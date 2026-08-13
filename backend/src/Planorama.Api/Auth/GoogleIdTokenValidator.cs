using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using Planorama.Api.Options;
using Planorama.Core.Exceptions;

namespace Planorama.Api.Auth;

/// <inheritdoc cref="IGoogleIdTokenValidator"/>
public class GoogleIdTokenValidator(IOptions<GoogleOptions> googleOptions) : IGoogleIdTokenValidator
{
    private readonly GoogleOptions _google = googleOptions.Value;

    public async Task<GoogleIdTokenPayload> ValidateAsync(string idToken, CancellationToken ct)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_google.ClientId],
            });
        }
        catch (InvalidJwtException)
        {
            throw new InvalidExternalTokenException();
        }

        return new GoogleIdTokenPayload(payload.Subject, payload.Email, payload.EmailVerified, payload.Name, payload.Picture);
    }
}
