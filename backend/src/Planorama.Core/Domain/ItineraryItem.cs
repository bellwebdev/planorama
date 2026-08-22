namespace Planorama.Core.Domain;

public class ItineraryItem
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }

    /// <summary>Null for a creator-pinned item with no suggestion behind it (not yet creatable via
    /// the API, but the schema supports it — see spec §5).</summary>
    public Guid? SuggestionId { get; set; }

    /// <summary>Null while unscheduled — sits in the creator's "unscheduled" tray until slotted.</summary>
    public DateOnly? Date { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Null = inherit <see cref="Trip.Timezone"/>. Per-item override enables multi-tz trips later.</summary>
    public string? Timezone { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Trip? Trip { get; set; }
    public Suggestion? Suggestion { get; set; }
}
