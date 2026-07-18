using Microsoft.AspNetCore.Identity;

namespace Planorama.Core.Domain;

public class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public UserSettings? Settings { get; set; }
}
