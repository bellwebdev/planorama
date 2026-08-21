using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Planorama.Core.Data;
using Planorama.Core.Domain;
using Planorama.Core.Exceptions;
using Planorama.Core.Integrations;
using Planorama.Core.Jobs;
using Planorama.Core.Options;
using Planorama.Core.Trips;

namespace Planorama.Core.Suggestions;

/// <inheritdoc cref="ISuggestionService"/>
public class SuggestionService(
    PlanoramaDbContext db,
    IPlacesProvider places,
    IGeocodingProvider geocoder,
    IBackgroundJobClient backgroundJobClient,
    IOptions<EmailOptions> emailOptions,
    TimeProvider timeProvider,
    ILogger<SuggestionService> logger) : ISuggestionService
{
    private readonly EmailOptions _email = emailOptions.Value;

    /// <inheritdoc/>
    public async Task<SuggestionResult> CreateAsync(Guid tripId, Guid userId, CreateSuggestionCommand command, CancellationToken ct)
    {
        Trip trip = await db.Trips
            .AccessibleBy(userId)
            .Where(t => t.Id == tripId)
            .FirstOrDefaultAsync(ct)
            ?? throw new TripNotFoundException();

        ResolvedPlace place = await ResolvePlaceAsync(trip, command, ct);

        var suggestion = new Suggestion
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            SuggestedById = userId,
            Source = command.ProviderPlaceId is null ? SuggestionSource.Custom : SuggestionSource.Geoapify,
            ProviderPlaceId = command.ProviderPlaceId,
            Title = place.Title,
            Description = command.Description,
            PlaceLat = place.Lat,
            PlaceLng = place.Lng,
            Address = place.Address,
            ExternalRating = place.Rating,
            ProposedDate = command.ProposedDate,
            ProposedStartTime = command.ProposedStartTime,
            DurationMinutes = command.DurationMinutes,
            VotingClosesAt = VotingWindow.Calculate(
                timeProvider.GetUtcNow(),
                command.VotingClosesAt,
                trip.DefaultVotingWindowHours,
                command.ProposedDate,
                command.ProposedStartTime,
                trip.StartDate,
                ResolveTimezone(trip.Timezone)),
            CreatedAt = timeProvider.GetUtcNow(),
        };

        db.Suggestions.Add(suggestion);
        await db.SaveChangesAsync(ct);

        await NotifyMembersAsync(trip, suggestion, userId, ct);

        return await GetByIdAsync(suggestion.Id, userId, ct);
    }

    /// <summary>Emails accepted trip members other than the suggester, skipping anyone who opted
    /// out via <see cref="UserSettings.NotifyEmail"/> — a member with no settings row yet has never
    /// touched their preferences, so the entity's default (opted in) applies (spec §8).</summary>
    private async Task NotifyMembersAsync(Trip trip, Suggestion suggestion, Guid suggesterId, CancellationToken ct)
    {
        List<(string Email, string DisplayName)> recipients = await db.TripMembers
            .Where(m => m.TripId == trip.Id && m.Status == TripMemberStatus.Accepted && m.UserId != suggesterId)
            .Join(db.Users, m => m.UserId, u => u.Id, (_, u) => u)
            .Where(u => u.Settings == null || u.Settings.NotifyEmail)
            .Select(u => new ValueTuple<string, string>(u.Email!, u.DisplayName))
            .ToListAsync(ct);

        var tripUrl = $"{_email.SuggestionUrlBase}/{trip.Id}";
        foreach ((string toEmail, string displayName) in recipients)
        {
            backgroundJobClient.Enqueue<IEmailDispatchJob>(
                j => j.SendSuggestionAddedAsync(toEmail, displayName, trip.Name, suggestion.Title, tripUrl));
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SuggestionResult>> ListForTripAsync(Guid tripId, Guid userId, CancellationToken ct)
    {
        bool isMember = await db.Trips.AccessibleBy(userId).AnyAsync(t => t.Id == tripId, ct);
        if (!isMember)
        {
            throw new TripNotFoundException();
        }

        List<SuggestionRow> rows = await QueryRows(db.Suggestions.Where(s => s.TripId == tripId), tripId).ToListAsync(ct);
        return rows.OrderByDescending(r => r.CreatedAt).Select(row => ToResult(row, userId)).ToList();
    }

    /// <inheritdoc/>
    public async Task<SuggestionResult> GetByIdAsync(Guid suggestionId, Guid userId, CancellationToken ct)
    {
        Guid tripId = await db.Suggestions
            .AsNoTracking()
            .AccessibleBy(userId)
            .Where(s => s.Id == suggestionId)
            .Select(s => s.TripId)
            .FirstOrDefaultAsync(ct);

        if (tripId == Guid.Empty)
        {
            throw new SuggestionNotFoundException();
        }

        SuggestionRow row = await QueryRows(db.Suggestions.Where(s => s.Id == suggestionId), tripId).FirstAsync(ct);
        return ToResult(row, userId);
    }

    /// <inheritdoc/>
    public async Task<SuggestionResult> CastVoteAsync(Guid suggestionId, Guid userId, VoteValue value, CancellationToken ct)
    {
        Suggestion suggestion = await db.Suggestions
            .AccessibleBy(userId)
            .Where(s => s.Id == suggestionId)
            .FirstOrDefaultAsync(ct)
            ?? throw new SuggestionNotFoundException();

        DateTimeOffset now = timeProvider.GetUtcNow();
        if (suggestion.Status != SuggestionStatus.Voting || now >= suggestion.VotingClosesAt)
        {
            throw new VotingClosedException();
        }

        Vote? existing = await db.Votes.FirstOrDefaultAsync(v => v.SuggestionId == suggestionId && v.UserId == userId, ct);
        if (existing is null)
        {
            db.Votes.Add(new Vote { SuggestionId = suggestionId, UserId = userId, Value = value, CastAt = now });
        }
        else
        {
            existing.Value = value;
            existing.CastAt = now;
        }

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(suggestionId, userId, ct);
    }

    /// <summary>Projects at the query level, filtering votes down to members still accepted on the
    /// trip — a member who left must not sway the count (spec §6 edge cases).</summary>
    private IQueryable<SuggestionRow> QueryRows(IQueryable<Suggestion> suggestions, Guid tripId) =>
        suggestions
            .AsNoTracking()
            .Select(s => new SuggestionRow(
                s.Id,
                s.TripId,
                s.SuggestedById,
                s.SuggestedBy!.DisplayName,
                s.Source,
                s.ProviderPlaceId,
                s.Title,
                s.Description,
                s.PlaceLat,
                s.PlaceLng,
                s.Address,
                s.ExternalRating,
                s.ProposedDate,
                s.ProposedStartTime,
                s.DurationMinutes,
                s.VotingClosesAt,
                s.Status,
                s.Resolution,
                s.ResolvedAt,
                s.CreatedAt,
                s.Votes
                    .Where(v => db.TripMembers.Any(m =>
                        m.TripId == tripId && m.UserId == v.UserId && m.Status == TripMemberStatus.Accepted))
                    .Select(v => new AttributedVote(v.UserId, v.User!.DisplayName, v.Value, v.CastAt))
                    .ToList()));

    /// <summary>Applies spec §6.3: tallies and attributed votes are withheld from a member who
    /// hasn't voted yet, so nobody can see which way the room is leaning before committing.
    /// Enforced here rather than in the endpoint or the UI — this is the only place it can't be
    /// bypassed.</summary>
    private static SuggestionResult ToResult(SuggestionRow row, Guid userId)
    {
        AttributedVote? own = row.Votes.FirstOrDefault(v => v.UserId == userId);
        bool hasVoted = own is not null;

        (int yes, int no) = VoteTally.Count(
            row.Votes.Select(v => new CountedVote(v.UserId, v.Value)).ToList(),
            row.SuggestedById);

        return new SuggestionResult(
            row.Id,
            row.TripId,
            row.SuggestedById,
            row.SuggestedByName,
            row.Source,
            row.ProviderPlaceId,
            row.Title,
            row.Description,
            row.PlaceLat,
            row.PlaceLng,
            row.Address,
            row.ExternalRating,
            row.ProposedDate,
            row.ProposedStartTime,
            row.DurationMinutes,
            row.VotingClosesAt,
            row.Status,
            row.Resolution,
            row.ResolvedAt,
            row.CreatedAt,
            hasVoted,
            own?.Value,
            hasVoted ? yes : null,
            hasVoted ? no : null,
            hasVoted ? row.Votes : null);
    }

    /// <summary>Re-fetches provider places server-side rather than trusting client-supplied
    /// coordinates; the 24h place-detail cache means this usually costs no provider credits.</summary>
    private async Task<ResolvedPlace> ResolvePlaceAsync(Trip trip, CreateSuggestionCommand command, CancellationToken ct)
    {
        if (command.ProviderPlaceId is { } providerPlaceId)
        {
            PlaceDetail detail = await places.GetDetailAsync(providerPlaceId, ct)
                ?? throw new SuggestionPlaceNotResolvedException("That place is no longer available from the provider.");

            return new ResolvedPlace(
                command.Title?.Trim() is { Length: > 0 } title ? title : detail.Name,
                detail.Location.Latitude,
                detail.Location.Longitude,
                detail.Address,
                detail.Rating);
        }

        if (command.Title?.Trim() is not { Length: > 0 } customTitle)
        {
            throw new SuggestionPlaceNotResolvedException("A custom suggestion needs a title.");
        }

        GeocodeResult? geocoded = command.Address is { Length: > 0 } address
            ? await TryGeocodeAsync(address, trip.LocationName, ct)
            : null;

        return new ResolvedPlace(
            customTitle,
            geocoded?.Location.Latitude,
            geocoded?.Location.Longitude,
            geocoded?.FormattedAddress ?? command.Address,
            Rating: null);
    }

    /// <summary>Best-effort, matching trip creation: an unresolvable address still produces a
    /// suggestion, it just can't show distance from the stay.</summary>
    private async Task<GeocodeResult?> TryGeocodeAsync(string address, string locationName, CancellationToken ct)
    {
        try
        {
            // Same disambiguation as trip stay addresses — a bare street matches dozens of cities.
            return await geocoder.GeocodeAsync($"{address}, {locationName}", ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Geocoding failed; suggestion coordinate left unresolved");
            return null;
        }
    }

    /// <summary>Trip timezones are stored as free text, so an unusable id must not take the
    /// request down — UTC keeps the clamp arithmetic sane and is logged for follow-up.</summary>
    private TimeZoneInfo ResolveTimezone(string timezone)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezone);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            logger.LogWarning(ex, "Unrecognised trip timezone {Timezone}; falling back to UTC", timezone);
            return TimeZoneInfo.Utc;
        }
    }

    private record ResolvedPlace(string Title, double? Lat, double? Lng, string? Address, decimal? Rating);

    private record SuggestionRow(
        Guid Id,
        Guid TripId,
        Guid SuggestedById,
        string SuggestedByName,
        SuggestionSource Source,
        string? ProviderPlaceId,
        string Title,
        string? Description,
        double? PlaceLat,
        double? PlaceLng,
        string? Address,
        decimal? ExternalRating,
        DateOnly? ProposedDate,
        TimeOnly? ProposedStartTime,
        int? DurationMinutes,
        DateTimeOffset VotingClosesAt,
        SuggestionStatus Status,
        SuggestionResolution? Resolution,
        DateTimeOffset? ResolvedAt,
        DateTimeOffset CreatedAt,
        List<AttributedVote> Votes);
}
