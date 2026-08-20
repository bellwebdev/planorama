using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Planorama.Core.Domain;

namespace Planorama.Core.Data;

/// <summary>Stores <see cref="SuggestionSource"/> as text ('custom' | 'geoapify').</summary>
public class SuggestionSourceConverter : ValueConverter<SuggestionSource, string>
{
    public static readonly SuggestionSourceConverter Instance = new();

    public SuggestionSourceConverter()
        : base(v => ToDb(v), v => FromDb(v))
    {
    }

    private static string ToDb(SuggestionSource value) => value switch
    {
        SuggestionSource.Custom => "custom",
        SuggestionSource.Geoapify => "geoapify",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static SuggestionSource FromDb(string value) => value switch
    {
        "custom" => SuggestionSource.Custom,
        "geoapify" => SuggestionSource.Geoapify,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}
