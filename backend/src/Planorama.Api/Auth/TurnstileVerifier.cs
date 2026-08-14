using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Planorama.Api.Options;
using Planorama.Core.Exceptions;

namespace Planorama.Api.Auth;

/// <inheritdoc cref="ITurnstileVerifier"/>
public class TurnstileVerifier(HttpClient httpClient, IOptions<TurnstileOptions> turnstileOptions) : ITurnstileVerifier
{
    private const string SiteVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    private readonly TurnstileOptions _turnstile = turnstileOptions.Value;

    /// <inheritdoc/>
    public async Task VerifyAsync(string? token, string? remoteIp, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidTurnstileTokenException();
        }

        var form = new Dictionary<string, string>
        {
            ["secret"] = _turnstile.SecretKey,
            ["response"] = token,
        };
        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            form["remoteip"] = remoteIp;
        }

        using var response = await httpClient.PostAsync(SiteVerifyUrl, new FormUrlEncodedContent(form), ct);
        var result = await response.Content.ReadFromJsonAsync<SiteVerifyResponse>(cancellationToken: ct);

        if (result is not { Success: true })
        {
            throw new InvalidTurnstileTokenException();
        }
    }

    private record SiteVerifyResponse([property: JsonPropertyName("success")] bool Success);
}
