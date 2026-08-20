using Planorama.Core.Domain;

namespace Planorama.Core.Suggestions;

public static class SuggestionQueryExtensions
{
    /// <summary>Restricts a suggestion query to suggestions on trips the user is an accepted member
    /// of. Mirrors <see cref="Trips.TripQueryExtensions.AccessibleBy"/> for the routes that address a
    /// suggestion directly and so have no trip id to check.</summary>
    /// <param name="suggestions">The suggestion query to constrain.</param>
    /// <param name="userId">The calling user.</param>
    /// <returns>The query, filtered to that user's accepted memberships.</returns>
    public static IQueryable<Suggestion> AccessibleBy(this IQueryable<Suggestion> suggestions, Guid userId) =>
        suggestions.Where(s => s.Trip!.Members.Any(m => m.UserId == userId && m.Status == TripMemberStatus.Accepted));
}
