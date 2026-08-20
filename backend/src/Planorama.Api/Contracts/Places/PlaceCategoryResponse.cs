using Planorama.Core.Places;

namespace Planorama.Api.Contracts.Places;

/// <summary>One selectable place category. Served from the API so the category list has a single
/// source of truth and the frontend never hardcodes a taxonomy.</summary>
public record PlaceCategoryResponse(PlaceCategory Value, string Label)
{
    private static readonly IReadOnlyDictionary<PlaceCategory, string> Labels = new Dictionary<PlaceCategory, string>
    {
        [PlaceCategory.Restaurant] = "Restaurants",
        [PlaceCategory.Cafe] = "Cafés",
        [PlaceCategory.Museum] = "Museums",
        [PlaceCategory.Attraction] = "Attractions",
        [PlaceCategory.Sights] = "Sights",
        [PlaceCategory.Park] = "Parks",
        [PlaceCategory.Playground] = "Playgrounds",
        [PlaceCategory.Beach] = "Beaches",
        [PlaceCategory.Zoo] = "Zoos",
        [PlaceCategory.ThemePark] = "Theme parks",
        [PlaceCategory.Shopping] = "Shopping",
        [PlaceCategory.Nature] = "Nature",
    };

    public static IReadOnlyList<PlaceCategoryResponse> All { get; } =
        Enum.GetValues<PlaceCategory>().Select(c => new PlaceCategoryResponse(c, Labels[c])).ToList();
}
