namespace Planorama.Core.Domain;

public class Trip
{
    public Guid Id { get; set; }
    public Guid CreatorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string LocationName { get; set; } = string.Empty;

    /// <summary>Null until geocoded — deferred to Phase 1b's Geoapify integration.</summary>
    public double? LocationLat { get; set; }
    public double? LocationLng { get; set; }

    public DateOnly StartDate { get; set; }

    /// <summary>Equal to <see cref="StartDate"/> for day trips.</summary>
    public DateOnly EndDate { get; set; }

    public string StayAddress { get; set; } = string.Empty;

    /// <summary>Null until geocoded — deferred to Phase 1b's Geoapify integration.</summary>
    public double? StayLat { get; set; }
    public double? StayLng { get; set; }

    public TripStatus Status { get; set; } = TripStatus.Draft;

    /// <summary>IANA timezone id, e.g. "America/New_York" — single timezone per trip in MVP.</summary>
    public string Timezone { get; set; } = string.Empty;

    public int DefaultVotingWindowHours { get; set; } = 48;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<TripMember> Members { get; set; } = [];
}
