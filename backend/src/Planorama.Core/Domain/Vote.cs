namespace Planorama.Core.Domain;

/// <summary>One member's vote on one suggestion. Changeable until the window closes, so this is
/// upserted rather than appended — there is never more than one row per (suggestion, member).</summary>
public class Vote
{
    public Guid SuggestionId { get; set; }
    public Guid UserId { get; set; }
    public VoteValue Value { get; set; }
    public DateTimeOffset CastAt { get; set; }

    public Suggestion? Suggestion { get; set; }
    public AppUser? User { get; set; }
}
