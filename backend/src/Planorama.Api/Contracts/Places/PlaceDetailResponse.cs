using Planorama.Core.Integrations;
using Planorama.Core.Places;

namespace Planorama.Api.Contracts.Places;

/// <inheritdoc cref="PlaceResponse"/>
public record PlaceDetailResponse(
    string ProviderPlaceId,
    string Name,
    double Lat,
    double Lng,
    PlaceCategory? Category,
    string? Address,
    string? Description,
    string? Website,
    decimal? Rating)
{
    public static PlaceDetailResponse FromResult(PlaceDetail place) => new(
        place.ProviderPlaceId, place.Name, place.Location.Latitude, place.Location.Longitude,
        place.Category, place.Address, place.Description, place.Website, place.Rating);
}
