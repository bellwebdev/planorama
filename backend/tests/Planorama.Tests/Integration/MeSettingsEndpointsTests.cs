using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Planorama.Api.Contracts.Me;
using Planorama.Core.Domain;
using Xunit;

namespace Planorama.Tests.Integration;

[Collection("Api")]
public class MeSettingsEndpointsTests(PlanoramaWebApplicationFactory factory)
{
    // Mirrors Program.cs's ConfigureHttpJsonOptions — the app's JSON options only govern the
    // server side of (de)serialization, so the test client needs its own matching setup: web
    // defaults (camelCase + case-insensitive matching, same as ASP.NET Core's own JsonOptions)
    // plus the JsonStringEnumConverter. Without JsonSerializerDefaults.Web, a plain `new()` here
    // silently fails to match the server's camelCase property names against this record's
    // PascalCase constructor parameters and falls back to each parameter's default value instead
    // of throwing — a false pass for any field whose default happens to equal the expected value.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_settings_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/v1/me/settings");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_settings_returns_defaults_for_new_user()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());

        var response = await AuthenticatedGetAsync("/api/v1/me/settings", login.Tokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await response.Content.ReadFromJsonAsync<SettingsResponse>(JsonOptions);
        Assert.NotNull(settings);
        Assert.Equal(ReminderOffset.TwelveHours, settings!.ReminderOffset);
        Assert.True(settings.NotifyEmail);
        Assert.False(settings.NotifyPush);
    }

    [Fact]
    public async Task Patch_settings_updates_all_fields()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());

        var patchResponse = await AuthenticatedPatchAsync("/api/v1/me/settings", login.Tokens.AccessToken,
            new UpdateSettingsRequest(ReminderOffset.OneHour, false, true));
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        var patched = await patchResponse.Content.ReadFromJsonAsync<SettingsResponse>(JsonOptions);
        Assert.Equal(ReminderOffset.OneHour, patched!.ReminderOffset);
        Assert.False(patched.NotifyEmail);
        Assert.True(patched.NotifyPush);

        var getResponse = await AuthenticatedGetAsync("/api/v1/me/settings", login.Tokens.AccessToken);
        var refetched = await getResponse.Content.ReadFromJsonAsync<SettingsResponse>(JsonOptions);
        Assert.Equal(ReminderOffset.OneHour, refetched!.ReminderOffset);
        Assert.False(refetched.NotifyEmail);
        Assert.True(refetched.NotifyPush);
    }

    [Fact]
    public async Task Patch_settings_with_invalid_reminder_offset_returns_400()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());

        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/me/settings")
        {
            Content = JsonContent.Create(new { ReminderOffset = "3h", NotifyEmail = true, NotifyPush = false }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.Tokens.AccessToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private Task<HttpResponseMessage> AuthenticatedGetAsync(string url, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return _client.SendAsync(request);
    }

    private Task<HttpResponseMessage> AuthenticatedPatchAsync<T>(string url, string accessToken, T body)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = JsonContent.Create(body, options: JsonOptions) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return _client.SendAsync(request);
    }
}
