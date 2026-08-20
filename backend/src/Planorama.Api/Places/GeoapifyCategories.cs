using Planorama.Core.Places;

namespace Planorama.Api.Places;

/// <summary>
/// Translates between Planorama's curated <see cref="PlaceCategory"/> and Geoapify's own dotted
/// category vocabulary. Confined to the adapter on purpose — vendor taxonomy must not reach the
/// API boundary, so swapping providers is a change to this file and its siblings only.
/// </summary>
internal static class GeoapifyCategories
{
    private static readonly IReadOnlyDictionary<PlaceCategory, string[]> ToProvider =
        new Dictionary<PlaceCategory, string[]>
        {
            [PlaceCategory.Restaurant] = ["catering.restaurant"],
            [PlaceCategory.Cafe] = ["catering.cafe"],
            [PlaceCategory.Museum] = ["entertainment.museum"],
            [PlaceCategory.Attraction] = ["tourism.attraction"],
            [PlaceCategory.Sights] = ["tourism.sights"],
            [PlaceCategory.Park] = ["leisure.park"],
            [PlaceCategory.Playground] = ["leisure.playground"],
            [PlaceCategory.Beach] = ["beach"],
            [PlaceCategory.Zoo] = ["entertainment.zoo"],
            [PlaceCategory.ThemePark] = ["entertainment.theme_park"],
            [PlaceCategory.Shopping] = ["commercial.shopping_mall"],
            [PlaceCategory.Nature] = ["natural.forest", "natural.mountain"],
        };

    /// <summary>Geoapify's <c>categories</c> parameter value for one of our categories.</summary>
    /// <param name="category">The category being searched.</param>
    /// <returns>A comma-separated list of provider category identifiers.</returns>
    internal static string ToQueryValue(PlaceCategory category) =>
        string.Join(',', ToProvider.TryGetValue(category, out string[]? mapped) ? mapped : ["tourism.attraction"]);

    /// <summary>Maps a result's provider categories back onto our enum.</summary>
    /// <param name="providerCategories">The <c>categories</c> array from a provider result.</param>
    /// <param name="requested">Category that was searched for, used when nothing matches — Geoapify
    /// tags results with many overlapping categories and some carry none we model.</param>
    /// <returns>The best-matching Planorama category.</returns>
    internal static PlaceCategory FromProvider(IEnumerable<string>? providerCategories, PlaceCategory requested)
    {
        if (providerCategories is null)
        {
            return requested;
        }

        var tags = providerCategories.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach ((PlaceCategory category, string[] identifiers) in ToProvider)
        {
            if (identifiers.Any(tags.Contains))
            {
                return category;
            }
        }

        return requested;
    }
}
