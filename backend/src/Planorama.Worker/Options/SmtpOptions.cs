namespace Planorama.Worker.Options;

/// <summary>Bound only in Planorama.Worker, never in Planorama.Api — reinforces the "email sent from worker only" rule at the DI level.</summary>
public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
