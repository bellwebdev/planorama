using Planorama.Core.Domain;

namespace Planorama.Api.Contracts.Me;

public record UpdateSettingsRequest(ReminderOffset ReminderOffset, bool NotifyEmail, bool NotifyPush);
