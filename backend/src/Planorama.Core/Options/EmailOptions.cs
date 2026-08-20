namespace Planorama.Core.Options;

/// <summary>Bound independently in both Planorama.Api (needs <see cref="ConfirmationUrlBase"/> to build the link) and Planorama.Worker (needs <see cref="FromAddress"/>/<see cref="FromName"/> to send it).</summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    public string FromAddress { get; set; } = "no-reply@planorama.family";
    public string FromName { get; set; } = "Planorama";

    /// <summary>Base URL the confirmation link is built against, e.g. "https://app.planorama.family/confirm-email".</summary>
    public string ConfirmationUrlBase { get; set; } = string.Empty;

    /// <summary>Base URL a trip invite accept link is built against, e.g. "https://app.planorama.family/invites/accept".</summary>
    public string InviteUrlBase { get; set; } = string.Empty;
}
