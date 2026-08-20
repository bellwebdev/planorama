using Planorama.Core.Integrations;

namespace Planorama.Api.Contracts.Places;

/// <summary>Query parameters for a route from the trip's stay address to a destination.</summary>
public record RouteRequest
{
    public double? ToLat { get; init; }

    public double? ToLng { get; init; }

    public TravelMode? Mode { get; init; }

    public TravelMode EffectiveMode => Mode ?? TravelMode.Drive;
}
