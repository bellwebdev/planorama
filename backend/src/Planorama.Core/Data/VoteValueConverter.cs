using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Planorama.Core.Domain;

namespace Planorama.Core.Data;

/// <summary>Stores <see cref="VoteValue"/> as text ('yes' | 'no').</summary>
public class VoteValueConverter : ValueConverter<VoteValue, string>
{
    public static readonly VoteValueConverter Instance = new();

    public VoteValueConverter()
        : base(v => ToDb(v), v => FromDb(v))
    {
    }

    private static string ToDb(VoteValue value) => value switch
    {
        VoteValue.Yes => "yes",
        VoteValue.No => "no",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static VoteValue FromDb(string value) => value switch
    {
        "yes" => VoteValue.Yes,
        "no" => VoteValue.No,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}
