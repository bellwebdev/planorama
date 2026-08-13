using Microsoft.EntityFrameworkCore;
using Planorama.Core.Data;
using Planorama.Core.Domain;
using Planorama.Core.Exceptions;

namespace Planorama.Core.Settings;

/// <inheritdoc cref="ISettingsService"/>
public class SettingsService(PlanoramaDbContext db) : ISettingsService
{
    /// <inheritdoc/>
    public async Task<SettingsResult> GetSettingsAsync(Guid userId, CancellationToken ct) =>
        ToResult(await GetOrCreateAsync(userId, ct));

    /// <inheritdoc/>
    public async Task<SettingsResult> UpdateSettingsAsync(Guid userId, ReminderOffset reminderOffset, bool notifyEmail, bool notifyPush, CancellationToken ct)
    {
        var settings = await GetOrCreateAsync(userId, ct);
        settings.ReminderOffset = reminderOffset;
        settings.NotifyEmail = notifyEmail;
        settings.NotifyPush = notifyPush;
        await db.SaveChangesAsync(ct);
        return ToResult(settings);
    }

    private async Task<UserSettings> GetOrCreateAsync(Guid userId, CancellationToken ct)
    {
        var settings = await db.UserSettings.FindAsync([userId], ct);
        if (settings is not null)
        {
            return settings;
        }

        if (!await db.Users.AnyAsync(u => u.Id == userId, ct))
        {
            throw new AccountNotFoundException();
        }

        settings = new UserSettings { UserId = userId };
        db.UserSettings.Add(settings);
        await db.SaveChangesAsync(ct);
        return settings;
    }

    private static SettingsResult ToResult(UserSettings settings) =>
        new(settings.ReminderOffset, settings.NotifyEmail, settings.NotifyPush);
}
