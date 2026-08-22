namespace Planorama.Core.Domain;

/// <summary>A scheduled event-reminder email for one member of an itinerary item (spec §8). Tracks
/// the Hangfire job id so a reschedule or cancellation can find and delete the pending job — Hangfire
/// has no way to address a job by anything other than the id it returned when scheduled.</summary>
public class Reminder
{
    public Guid Id { get; set; }
    public Guid ItineraryItemId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset ScheduledForUtc { get; set; }
    public string HangfireJobId { get; set; } = string.Empty;

    public ItineraryItem? ItineraryItem { get; set; }
}
