using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Planorama.Core.Domain;

namespace Planorama.Core.Data;

public class PlanoramaDbContext(DbContextOptions<PlanoramaDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripMember> TripMembers => Set<TripMember>();
    public DbSet<Invitation> Invitations => Set<Invitation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(user =>
        {
            user.Property(u => u.DisplayName).HasMaxLength(100);
            user.Property(u => u.AvatarUrl).HasMaxLength(500);
        });

        builder.Entity<UserSettings>(settings =>
        {
            settings.HasKey(s => s.UserId);
            settings.Property(s => s.ReminderOffset)
                .HasConversion(ReminderOffsetConverter.Instance)
                .HasMaxLength(3);
            settings.HasOne(s => s.User)
                .WithOne(u => u.Settings)
                .HasForeignKey<UserSettings>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RefreshToken>(token =>
        {
            token.HasKey(t => t.Id);
            token.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
            token.HasIndex(t => t.TokenHash).IsUnique();
            token.HasIndex(t => t.FamilyId);
            token.HasIndex(t => t.UserId);
            token.Property(t => t.RevocationReason).HasConversion(RefreshTokenRevocationReasonConverter.Instance).HasMaxLength(20);
            token.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<IdempotencyKey>(key =>
        {
            key.HasKey(k => new { k.Endpoint, k.Key });
        });

        builder.Entity<Trip>(trip =>
        {
            trip.Property(t => t.Name).HasMaxLength(200).IsRequired();
            trip.Property(t => t.LocationName).HasMaxLength(200).IsRequired();
            trip.Property(t => t.StayAddress).HasMaxLength(300).IsRequired();
            trip.Property(t => t.Timezone).HasMaxLength(100).IsRequired();
            trip.Property(t => t.Status).HasConversion(TripStatusConverter.Instance).HasMaxLength(20);
            trip.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(t => t.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);
            trip.HasIndex(t => t.CreatorId);
        });

        builder.Entity<TripMember>(member =>
        {
            member.HasKey(m => new { m.TripId, m.UserId });
            member.Property(m => m.Role).HasConversion(TripMemberRoleConverter.Instance).HasMaxLength(10);
            member.Property(m => m.Status).HasConversion(TripMemberStatusConverter.Instance).HasMaxLength(10);
            member.Property(m => m.InvitedVia).HasConversion(NullableInvitedViaConverter.Instance).HasMaxLength(10);
            member.HasOne(m => m.Trip)
                .WithMany(t => t.Members)
                .HasForeignKey(m => m.TripId)
                .OnDelete(DeleteBehavior.Cascade);
            member.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            member.HasIndex(m => m.UserId);
        });

        builder.Entity<Invitation>(invitation =>
        {
            invitation.Property(i => i.InvitedVia).HasConversion(InvitedViaConverter.Instance).HasMaxLength(10);
            invitation.Property(i => i.Contact).HasMaxLength(320);
            invitation.HasIndex(i => i.Token).IsUnique();
            invitation.HasOne(i => i.Trip)
                .WithMany()
                .HasForeignKey(i => i.TripId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
