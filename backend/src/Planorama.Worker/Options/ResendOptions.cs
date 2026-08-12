namespace Planorama.Worker.Options;

/// <summary>Bound only in Planorama.Worker, never in Planorama.Api — reinforces the "email sent from worker only" rule at the DI level.</summary>
public class ResendOptions
{
    public const string SectionName = "Resend";

    public string ApiKey { get; set; } = string.Empty;
}
