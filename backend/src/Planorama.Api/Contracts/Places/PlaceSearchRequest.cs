using Planorama.Core.Places;

namespace Planorama.Api.Contracts.Places;

/// <summary>Query parameters for a nearby-places search. Every field is nullable so an omitted
/// parameter is distinguishable from a supplied zero, and defaults live in one place.</summary>
public record PlaceSearchRequest
{
    public const int DefaultRadiusMeters = 5_000;
    public const int MinRadiusMeters = 100;
    public const int MaxRadiusMeters = 50_000;
    public const int DefaultLimit = 20;
    public const int MaxLimit = 50;
    public const int MaxNameFilterLength = 100;

    public PlaceCategory? Category { get; init; }

    public int? Radius { get; init; }

    /// <summary>Optional name filter. This narrows results within the chosen category — it is not
    /// a free-text search across all places.</summary>
    public string? Q { get; init; }

    public int? Limit { get; init; }

    public int EffectiveRadius => Radius ?? DefaultRadiusMeters;

    public int EffectiveLimit => Limit ?? DefaultLimit;
}
