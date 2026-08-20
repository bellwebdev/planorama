using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Planorama.Core.Exceptions;

namespace Planorama.Api.Places;

/// <summary>Shared request plumbing for the three Geoapify adapters: URL assembly with the API key
/// attached server-side, and one place where transport failures become
/// <see cref="ProviderUnavailableException"/> instead of leaking HTTP types upward.</summary>
internal static class GeoapifyClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Formats a coordinate component for a query string, pinned to invariant culture —
    /// a comma decimal separator would silently corrupt every request on a non-English host.</summary>
    /// <param name="value">The coordinate component.</param>
    /// <returns>The invariant string form.</returns>
    internal static string Coord(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    /// <summary>Issues a GET and deserializes the response.</summary>
    /// <typeparam name="T">Shape to deserialize into.</typeparam>
    /// <param name="httpClient">The typed client for this provider.</param>
    /// <param name="path">Absolute URL without the API key.</param>
    /// <param name="parameters">Query parameters; the API key is appended here, never by callers.</param>
    /// <param name="apiKey">The Geoapify API key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The deserialized response, or <c>null</c> when the body is empty.</returns>
    /// <exception cref="ProviderUnavailableException">Non-success status, timeout, or an unparseable body.</exception>
    internal static async Task<T?> GetAsync<T>(
        HttpClient httpClient, string path, Dictionary<string, string?> parameters, string apiKey, CancellationToken ct)
    {
        parameters["apiKey"] = apiKey;
        string url = QueryHelpers.AddQueryString(path, parameters);

        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new ProviderUnavailableException($"Geoapify responded {(int)response.StatusCode}.");
            }

            return await response.Content.ReadFromJsonAsync<T>(SerializerOptions, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new ProviderUnavailableException("Couldn't reach the place provider.", ex);
        }
        catch (JsonException ex)
        {
            throw new ProviderUnavailableException("The place provider returned an unexpected response.", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new ProviderUnavailableException("The place provider timed out.", ex);
        }
    }
}
