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
    }
}
