namespace Planorama.Core.Domain;

public class UserSettings
{
    public Guid UserId { get; set; }
    public ReminderOffset ReminderOffset { get; set; } = ReminderOffset.TwelveHours;
    public bool NotifyEmail { get; set; } = true;
    public bool NotifyPush { get; set; }

    public AppUser? User { get; set; }
}
