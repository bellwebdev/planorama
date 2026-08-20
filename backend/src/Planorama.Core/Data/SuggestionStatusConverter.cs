using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Planorama.Core.Domain;

namespace Planorama.Core.Data;

/// <summary>Stores <see cref="SuggestionStatus"/> as text ('voting' | 'approved' | 'discarded' | 'expired').</summary>
public class SuggestionStatusConverter : ValueConverter<SuggestionStatus, string>
{
    public static readonly SuggestionStatusConverter Instance = new();

    public SuggestionStatusConverter()
        : base(v => ToDb(v), v => FromDb(v))
    {
    }

    private static string ToDb(SuggestionStatus value) => value switch
    {
        SuggestionStatus.Voting => "voting",
        SuggestionStatus.Approved => "approved",
        SuggestionStatus.Discarded => "discarded",
        SuggestionStatus.Expired => "expired",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static SuggestionStatus FromDb(string value) => value switch
    {
        "voting" => SuggestionStatus.Voting,
        "approved" => SuggestionStatus.Approved,
        "discarded" => SuggestionStatus.Discarded,
        "expired" => SuggestionStatus.Expired,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}
