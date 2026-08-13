using Planorama.Core.Domain;

namespace Planorama.Core.Settings;

public record SettingsResult(ReminderOffset ReminderOffset, bool NotifyEmail, bool NotifyPush);
