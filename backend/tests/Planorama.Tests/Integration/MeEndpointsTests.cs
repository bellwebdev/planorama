using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Planorama.Api.Contracts.Me;
using SkiaSharp;
using Xunit;

namespace Planorama.Tests.Integration;

[Collection("Api")]
public class MeEndpointsTests(PlanoramaWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_me_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_me_returns_current_profile()
    {
        var email = AuthTestHelpers.UniqueEmail();
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, email);

        var response = await AuthenticatedGetAsync("/api/v1/me", login.Tokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(me);
        Assert.Equal(email, me!.Email);
        Assert.Null(me.AvatarUrl);
    }

    [Fact]
    public async Task Patch_me_updates_display_name()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());

        var response = await AuthenticatedPatchAsync("/api/v1/me", login.Tokens.AccessToken, new UpdateProfileRequest("Grace Hopper"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.Equal("Grace Hopper", me!.DisplayName);
    }

    [Fact]
    public async Task Patch_me_with_empty_display_name_returns_400_validation_problem()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());

        var response = await AuthenticatedPatchAsync("/api/v1/me", login.Tokens.AccessToken, new UpdateProfileRequest(""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_avatar_happy_path_returns_new_avatar_url()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());

        var response = await PostAvatarAsync(login.Tokens.AccessToken, GenerateSolidColorPng(800, 600, SKColors.CornflowerBlue));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(me!.AvatarUrl);
    }

    [Fact]
    public async Task Post_avatar_oversized_file_returns_413()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());

        var response = await PostAvatarAsync(login.Tokens.AccessToken, new byte[6 * 1024 * 1024]);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task Post_avatar_non_image_file_returns_400()
    {
        var login = await AuthTestHelpers.RegisterConfirmAndLoginAsync(_client, factory, AuthTestHelpers.UniqueEmail());

        var response = await PostAvatarAsync(login.Tokens.AccessToken, [1, 2, 3, 4, 5]);
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
        var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return _client.SendAsync(request);
    }

    private Task<HttpResponseMessage> PostAvatarAsync(string accessToken, byte[] fileBytes)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/me/avatar");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var content = new MultipartFormDataContent();
        var filePart = new ByteArrayContent(fileBytes);
        filePart.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(filePart, "file", "avatar.png");
        request.Content = content;

        return _client.SendAsync(request);
    }

    private static byte[] GenerateSolidColorPng(int width, int height, SKColor color)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(color);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
