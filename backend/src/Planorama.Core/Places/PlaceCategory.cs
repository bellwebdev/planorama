namespace Planorama.Core.Places;

/// <summary>
/// The curated set of place types Planorama exposes. Deliberately small and family-oriented rather
/// than a passthrough of the provider's taxonomy — provider category strings are mapped to and from
/// this enum inside the provider adapter so no vendor vocabulary reaches the API boundary.
/// </summary>
public enum PlaceCategory
{
    Restaurant,
    Cafe,
    Museum,
    Attraction,
    Sights,
    Park,
    Playground,
    Beach,
    Zoo,
    ThemePark,
    Shopping,
    Nature,
}
