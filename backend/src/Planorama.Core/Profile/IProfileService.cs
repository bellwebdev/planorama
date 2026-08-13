namespace Planorama.Core.Profile;

public interface IProfileService
{
    /// <exception cref="Exceptions.AccountNotFoundException">The JWT's subject no longer maps to an existing account.</exception>
    Task<ProfileResult> GetProfileAsync(Guid userId, CancellationToken ct);

    /// <exception cref="Exceptions.AccountNotFoundException">The JWT's subject no longer maps to an existing account.</exception>
    Task<ProfileResult> UpdateDisplayNameAsync(Guid userId, string displayName, CancellationToken ct);

    /// <exception cref="Exceptions.AccountNotFoundException">The JWT's subject no longer maps to an existing account.</exception>
    /// <exception cref="Exceptions.AvatarTooLargeException"><paramref name="declaredLength"/> exceeds 5 MB.</exception>
    /// <exception cref="Exceptions.UnsupportedImageFormatException">The stream isn't a decodable, supported image.</exception>
    Task<ProfileResult> UpdateAvatarAsync(Guid userId, Stream imageStream, long declaredLength, CancellationToken ct);
}
