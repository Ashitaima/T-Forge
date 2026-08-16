using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class MatchChallengeStatusTests
{
    [Theory]
    [InlineData("Pending")]
    [InlineData("Accepted")]
    [InlineData("Declined")]
    [InlineData("Cancelled")]
    public void IsValid_KnownStatus_IsTrue(string status)
    {
        Assert.True(MatchChallengeStatus.IsValid(status));
    }

    [Theory]
    [InlineData("Expired")]
    [InlineData("pending")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_UnknownStatus_IsFalse(string? status)
    {
        Assert.False(MatchChallengeStatus.IsValid(status));
    }

    // Значення збігаються зі статусами запитів на членство — фронтенд
    // показує обидва однаково, тож розходження було б помилкою.
    [Fact]
    public void Values_MatchMembershipRequestStatuses()
    {
        Assert.Equal(MembershipRequestStatus.All, MatchChallengeStatus.All);
    }
}
