using Microsoft.AspNetCore.Identity;
using Planorama.Core.Domain;
using Planorama.Core.Exceptions;
using Planorama.Core.Media;

namespace Planorama.Core.Profile;

/// <inheritdoc cref="IProfileService"/>
public class ProfileService(
    UserManager<AppUser> userManager,
    IImageProcessor imageProcessor,
    IAvatarStorage avatarStorage) : IProfileService
{
    private const long MaxAvatarBytes = 5 * 1024 * 1024;

    /// <inheritdoc/>
    public async Task<ProfileResult> GetProfileAsync(Guid userId, CancellationToken ct) =>
        ToResult(await FindUserAsync(userId));

    /// <inheritdoc/>
    public async Task<ProfileResult> UpdateDisplayNameAsync(Guid userId, string displayName, CancellationToken ct)
    {
        var user = await FindUserAsync(userId);
        user.DisplayName = displayName;
        await userManager.UpdateAsync(user);
        return ToResult(user);
    }

    /// <inheritdoc/>
    public async Task<ProfileResult> UpdateAvatarAsync(Guid userId, Stream imageStream, long declaredLength, CancellationToken ct)
    {
        // Cheap check first, before the stream is ever touched — avoids allocating/decoding for
        // an oversized upload that's going to be rejected regardless.
        if (declaredLength > MaxAvatarBytes)
        {
            throw new AvatarTooLargeException();
        }

        var user = await FindUserAsync(userId);
        var processed = imageProcessor.ProcessAvatar(imageStream);

        user.AvatarUrl = await avatarStorage.SaveAsync(userId, processed.Bytes, processed.ContentType, ct);
        await userManager.UpdateAsync(user);

        return ToResult(user);
    }

    private async Task<AppUser> FindUserAsync(Guid userId) =>
        await userManager.FindByIdAsync(userId.ToString()) ?? throw new AccountNotFoundException();

    private static ProfileResult ToResult(AppUser user) =>
        new(user.Id, user.Email!, user.DisplayName, user.AvatarUrl, user.CreatedAt); // Email is non-null — see AuthService.RegisterAsync.
}
