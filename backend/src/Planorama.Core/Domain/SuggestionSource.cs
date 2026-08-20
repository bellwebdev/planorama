namespace Planorama.Core.Domain;

/// <summary>Where a suggestion's place data came from. Only the two reachable sources are modelled —
/// a new provider adds a value here and a case to its converter.</summary>
public enum SuggestionSource
{
    Custom,
    Geoapify,
}
