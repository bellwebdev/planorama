using Planorama.Core.Integrations;
using Planorama.Core.Places;

namespace Planorama.Api.Contracts.Places;

/// <param name="Rating">Always null while the provider is OpenStreetMap-derived; see <see cref="Places.GeoapifyPlacesProvider"/>.</param>
public record PlaceResponse(
    string ProviderPlaceId,
    string Name,
    double Lat,
    double Lng,
    PlaceCategory Category,
    string? Address,
    int? DistanceMeters,
    decimal? Rating)
{
    public static PlaceResponse FromResult(PlaceResult place) => new(
        place.ProviderPlaceId, place.Name, place.Location.Latitude, place.Location.Longitude,
        place.Category, place.Address, place.DistanceMeters, place.Rating);
}
