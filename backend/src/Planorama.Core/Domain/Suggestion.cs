namespace Planorama.Core.Domain;

public class Suggestion
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public Guid SuggestedById { get; set; }

    public SuggestionSource Source { get; set; }

    /// <summary>The provider's own place id when <see cref="Source"/> isn't <see cref="SuggestionSource.Custom"/>.</summary>
    public string? ProviderPlaceId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Null when the address couldn't be geocoded — the suggestion still stands, it just
    /// can't show distance or directions from the stay.</summary>
    public double? PlaceLat { get; set; }
    public double? PlaceLng { get; set; }

    public string? Address { get; set; }

    /// <summary>Always null on OpenStreetMap-derived providers; kept for a ratings-carrying provider.</summary>
    public decimal? ExternalRating { get; set; }

    public DateOnly? ProposedDate { get; set; }
    public TimeOnly? ProposedStartTime { get; set; }
    public int? DurationMinutes { get; set; }

    /// <summary>When voting closes. Computed server-side per spec §6.1 — never taken raw from a client.</summary>
    public DateTimeOffset VotingClosesAt { get; set; }

    public SuggestionStatus Status { get; set; } = SuggestionStatus.Voting;

    /// <summary>Set by the Phase 2 worker resolution job or a creator override; null while voting.</summary>
    public SuggestionResolution? Resolution { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Trip? Trip { get; set; }
    public AppUser? SuggestedBy { get; set; }
    public ICollection<Vote> Votes { get; set; } = [];
}
