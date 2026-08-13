using Planorama.Core.Domain;

namespace Planorama.Core.Settings;

public interface ISettingsService
{
    /// <summary>First-touch get-or-create: a user with no settings row yet gets one created with the entity's defaults.</summary>
    /// <exception cref="Exceptions.AccountNotFoundException">The JWT's subject no longer maps to an existing account.</exception>
    Task<SettingsResult> GetSettingsAsync(Guid userId, CancellationToken ct);

    /// <exception cref="Exceptions.AccountNotFoundException">The JWT's subject no longer maps to an existing account.</exception>
    Task<SettingsResult> UpdateSettingsAsync(Guid userId, ReminderOffset reminderOffset, bool notifyEmail, bool notifyPush, CancellationToken ct);
}
