using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Planorama.Core.Domain;

namespace Planorama.Core.Data;

public class PlanoramaDbContext(DbContextOptions<PlanoramaDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

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
    }
}
