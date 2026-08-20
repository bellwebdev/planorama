using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Planorama.Core.Domain;

namespace Planorama.Core.Data;

/// <summary>Stores <see cref="SuggestionResolution"/> as text ('majority' | 'coin_flip' |
/// 'no_quorum' | 'manual'); null until the suggestion is resolved.</summary>
public class NullableSuggestionResolutionConverter : ValueConverter<SuggestionResolution?, string?>
{
    public static readonly NullableSuggestionResolutionConverter Instance = new();

    public NullableSuggestionResolutionConverter()
        : base(v => ToDb(v), v => FromDb(v))
    {
    }

    private static string? ToDb(SuggestionResolution? value) => value switch
    {
        null => null,
        SuggestionResolution.Majority => "majority",
        SuggestionResolution.CoinFlip => "coin_flip",
        SuggestionResolution.NoQuorum => "no_quorum",
        SuggestionResolution.Manual => "manual",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static SuggestionResolution? FromDb(string? value) => value switch
    {
        null => null,
        "majority" => SuggestionResolution.Majority,
        "coin_flip" => SuggestionResolution.CoinFlip,
        "no_quorum" => SuggestionResolution.NoQuorum,
        "manual" => SuggestionResolution.Manual,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}
