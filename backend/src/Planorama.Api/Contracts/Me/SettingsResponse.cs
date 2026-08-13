using Planorama.Core.Domain;

namespace Planorama.Api.Contracts.Me;

public record SettingsResponse(ReminderOffset ReminderOffset, bool NotifyEmail, bool NotifyPush);
