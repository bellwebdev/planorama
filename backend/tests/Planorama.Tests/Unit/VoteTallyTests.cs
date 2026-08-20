using Planorama.Core.Domain;
using Planorama.Core.Suggestions;
using Xunit;

namespace Planorama.Tests.Unit;

public class VoteTallyTests
{
    private static readonly Guid Suggester = Guid.NewGuid();
    private static readonly Guid Alice = Guid.NewGuid();
    private static readonly Guid Bob = Guid.NewGuid();

    [Fact]
    public void Excludes_the_suggesters_lone_vote_so_nobody_self_approves()
    {
        (int yes, int no) = VoteTally.Count([new CountedVote(Suggester, VoteValue.Yes)], Suggester);

        Assert.Equal(0, yes);
        Assert.Equal(0, no);
    }

    [Fact]
    public void Counts_the_suggesters_vote_once_someone_else_has_voted()
    {
        (int yes, int no) = VoteTally.Count(
            [new CountedVote(Suggester, VoteValue.Yes), new CountedVote(Alice, VoteValue.Yes)],
            Suggester);

        Assert.Equal(2, yes);
        Assert.Equal(0, no);
    }

    [Fact]
    public void Counts_a_lone_vote_from_someone_who_is_not_the_suggester()
    {
        (int yes, int no) = VoteTally.Count([new CountedVote(Alice, VoteValue.No)], Suggester);

        Assert.Equal(0, yes);
        Assert.Equal(1, no);
    }

    [Fact]
    public void Counts_a_mixed_room()
    {
        (int yes, int no) = VoteTally.Count(
            [
                new CountedVote(Suggester, VoteValue.Yes),
                new CountedVote(Alice, VoteValue.No),
                new CountedVote(Bob, VoteValue.No),
            ],
            Suggester);

        Assert.Equal(1, yes);
        Assert.Equal(2, no);
    }

    [Fact]
    public void Counts_nothing_when_nobody_has_voted()
    {
        (int yes, int no) = VoteTally.Count([], Suggester);

        Assert.Equal(0, yes);
        Assert.Equal(0, no);
    }
}
