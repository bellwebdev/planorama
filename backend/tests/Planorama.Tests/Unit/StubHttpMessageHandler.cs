using System.Net;
using System.Text;

namespace Planorama.Tests.Unit;

/// <summary>Returns a canned response for any request, capturing the URL so tests can assert on
/// query-string assembly (culture-sensitive coordinate formatting in particular).</summary>
public class StubHttpMessageHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
{
    public Uri? LastRequestUri { get; private set; }

    /// <summary>The last request's query string, percent-decoding undone for readable assertions.</summary>
    public string LastQuery => Uri.UnescapeDataString(LastRequestUri?.Query ?? string.Empty);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;
        return Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });
    }
}
