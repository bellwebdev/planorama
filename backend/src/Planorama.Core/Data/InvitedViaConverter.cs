using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Planorama.Core.Domain;

namespace Planorama.Core.Data;

/// <summary>Stores <see cref="InvitedVia"/> as text ('email' | 'sms' | 'link').</summary>
public class InvitedViaConverter : ValueConverter<InvitedVia, string>
{
    public static readonly InvitedViaConverter Instance = new();

    public InvitedViaConverter()
        : base(v => ToDb(v), v => FromDb(v))
    {
    }

    /// <summary>Internal so <see cref="NullableInvitedViaConverter"/> can share the same mapping.</summary>
    internal static string ToDb(InvitedVia value) => value switch
    {
        InvitedVia.Email => "email",
        InvitedVia.Sms => "sms",
        InvitedVia.Link => "link",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    internal static InvitedVia FromDb(string value) => value switch
    {
        "email" => InvitedVia.Email,
        "sms" => InvitedVia.Sms,
        "link" => InvitedVia.Link,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}
