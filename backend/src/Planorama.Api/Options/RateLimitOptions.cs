namespace Planorama.Api.Options;

public class RateLimitPolicyOptions
{
    public int PermitLimit { get; set; }
    public int WindowMinutes { get; set; }
}

/// <summary>Config-bound so tests can raise the limits well above anything a shared test run could
/// hit, instead of tripping the same per-IP buckets production traffic uses.</summary>
public class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public RateLimitPolicyOptions AuthRegister { get; set; } = new() { PermitLimit = 5, WindowMinutes = 15 };
    public RateLimitPolicyOptions AuthResendConfirmation { get; set; } = new() { PermitLimit = 3, WindowMinutes = 15 };
}
