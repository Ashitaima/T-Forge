using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class MatchChallengePolicyTests
{
    private const int ChallengerCaptain = 10;
    private const int OpponentCaptain = 20;
    private const int Stranger = 30;

    private static MatchChallengePolicy.Context Challenge(
        string status = MatchChallengeStatus.Pending) =>
        new(status, ChallengerCaptain, ChallengerCaptain, OpponentCaptain);

    // ---- Хто відповідає ----

    [Fact]
    public void ResponderUserId_IsTheOpponentCaptain()
    {
        Assert.Equal(OpponentCaptain, MatchChallengePolicy.ResponderUserId(Challenge()));
    }

    // ---- CanRespond ----

    [Fact]
    public void CanRespond_OpponentCaptainMay()
    {
        Assert.True(MatchChallengePolicy.CanRespond(Challenge(), OpponentCaptain, isAdmin: false));
    }

    // Ініціатор не може прийняти власний виклик — інакше матч створювався б однобічно.
    [Fact]
    public void CanRespond_InitiatorMayNot()
    {
        Assert.False(MatchChallengePolicy.CanRespond(Challenge(), ChallengerCaptain, isAdmin: false));
    }

    [Fact]
    public void CanRespond_StrangerMayNot()
    {
        Assert.False(MatchChallengePolicy.CanRespond(Challenge(), Stranger, isAdmin: false));
    }

    [Fact]
    public void CanRespond_AdminMay()
    {
        Assert.True(MatchChallengePolicy.CanRespond(Challenge(), Stranger, isAdmin: true));
    }

    [Theory]
    [InlineData(MatchChallengeStatus.Accepted)]
    [InlineData(MatchChallengeStatus.Declined)]
    [InlineData(MatchChallengeStatus.Cancelled)]
    public void CanRespond_TerminalStatus_IsRefusedEvenForAdmin(string status)
    {
        Assert.False(MatchChallengePolicy.CanRespond(Challenge(status), OpponentCaptain, isAdmin: false));
        Assert.False(MatchChallengePolicy.CanRespond(Challenge(status), OpponentCaptain, isAdmin: true));
    }

    // ---- CanCancel ----

    [Fact]
    public void CanCancel_InitiatorMay()
    {
        Assert.True(MatchChallengePolicy.CanCancel(Challenge(), ChallengerCaptain, isAdmin: false));
    }

    [Fact]
    public void CanCancel_OpponentMayNot()
    {
        Assert.False(MatchChallengePolicy.CanCancel(Challenge(), OpponentCaptain, isAdmin: false));
    }

    [Fact]
    public void CanCancel_AdminMay()
    {
        Assert.True(MatchChallengePolicy.CanCancel(Challenge(), Stranger, isAdmin: true));
    }

    [Fact]
    public void CanCancel_TerminalStatus_IsRefused()
    {
        Assert.False(MatchChallengePolicy.CanCancel(
            Challenge(MatchChallengeStatus.Accepted), ChallengerCaptain, isAdmin: true));
    }

    // ---- IsPending ----

    [Fact]
    public void IsPending_OnlyForPending()
    {
        Assert.True(MatchChallengePolicy.IsPending(Challenge()));
        Assert.False(MatchChallengePolicy.IsPending(Challenge(MatchChallengeStatus.Declined)));
    }

    // ---- Відкритий виклик ----
    //
    // Суперника не названо, тож прийняти може капітан будь-якої іншої
    // команди — той самий поділ, що в DuelPolicy.

    private static MatchChallengePolicy.Context Open(
        string status = MatchChallengeStatus.Pending) =>
        new(status, ChallengerCaptain, ChallengerCaptain, null);

    [Fact]
    public void IsOpen_WhenOpponentIsUnnamed()
    {
        Assert.True(MatchChallengePolicy.IsOpen(Open()));
        Assert.False(MatchChallengePolicy.IsOpen(Challenge()));
    }

    [Fact]
    public void Open_HasNoNamedResponder()
    {
        Assert.Null(MatchChallengePolicy.ResponderUserId(Open()));
    }

    [Fact]
    public void Open_AnyOtherCaptainMayRespond()
    {
        Assert.True(MatchChallengePolicy.CanRespond(Open(), Stranger, isAdmin: false));
        Assert.True(MatchChallengePolicy.CanRespond(Open(), OpponentCaptain, isAdmin: false));
    }

    // Прийняти власний виклик не можна в жодному разі: інакше згоди другої
    // сторони не існувало б — і це стосується адміністратора теж.
    [Fact]
    public void Open_InitiatorMayNotRespond_EvenAsAdmin()
    {
        Assert.False(MatchChallengePolicy.CanRespond(Open(), ChallengerCaptain, isAdmin: false));
        Assert.False(MatchChallengePolicy.CanRespond(Open(), ChallengerCaptain, isAdmin: true));
    }

    [Fact]
    public void Open_TerminalStatus_IsRefused()
    {
        Assert.False(MatchChallengePolicy.CanRespond(
            Open(MatchChallengeStatus.Accepted), Stranger, isAdmin: false));
    }

    [Fact]
    public void Open_InitiatorMayStillCancel()
    {
        Assert.True(MatchChallengePolicy.CanCancel(Open(), ChallengerCaptain, isAdmin: false));
    }
}
