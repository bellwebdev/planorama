using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Planorama.Core.Domain;

namespace Planorama.Core.Data;

/// <summary>
/// Nullable counterpart to <see cref="InvitedViaConverter"/> — <see cref="Domain.TripMember.InvitedVia"/>
/// is null for the creator's own membership row (created directly, not via an invite).
/// </summary>
public class NullableInvitedViaConverter : ValueConverter<InvitedVia?, string?>
{
    public static readonly NullableInvitedViaConverter Instance = new();

    public NullableInvitedViaConverter()
        : base(v => v == null ? null : InvitedViaConverter.ToDb(v.Value), v => v == null ? null : InvitedViaConverter.FromDb(v))
    {
    }
}
