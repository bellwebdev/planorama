using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Planorama.Tests.Integration;

/// <summary>Bearer-token request helpers shared by every endpoint test class.</summary>
public static class AuthenticatedRequests
{
    public static Task<HttpResponseMessage> AuthenticatedGetAsync(this HttpClient client, string url, string accessToken) =>
        client.SendAsync(Authenticated(new HttpRequestMessage(HttpMethod.Get, url), accessToken));

    public static Task<HttpResponseMessage> AuthenticatedPatchAsync<T>(this HttpClient client, string url, string accessToken, T body) =>
        client.SendAsync(Authenticated(new HttpRequestMessage(HttpMethod.Patch, url) { Content = JsonContent.Create(body) }, accessToken));

    public static Task<HttpResponseMessage> AuthenticatedPutAsync<T>(this HttpClient client, string url, string accessToken, T body) =>
        client.SendAsync(Authenticated(new HttpRequestMessage(HttpMethod.Put, url) { Content = JsonContent.Create(body) }, accessToken));

    public static Task<HttpResponseMessage> AuthenticatedPostAsync(this HttpClient client, string url, string accessToken) =>
        client.SendAsync(Authenticated(new HttpRequestMessage(HttpMethod.Post, url), accessToken));

    public static Task<HttpResponseMessage> AuthenticatedPostAsync<T>(
        this HttpClient client, string url, string accessToken, T body, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return client.SendAsync(Authenticated(request, accessToken));
    }

    private static HttpRequestMessage Authenticated(HttpRequestMessage request, string accessToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }
}
