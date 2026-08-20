using TForge.Common;
using TForge.Models;

namespace TForge.Tests;

/// <summary>
/// Статуси дуелі. Перелік окремий від MatchStatus навмисно — тест тримає той
/// поділ, щоб Pending і Postponed не переїхали одне до одного.
/// </summary>
public class DuelStatusesTests
{
    [Fact]
    public void All_HoldsEveryDeclaredStatus()
    {
        Assert.Equal(
            new[] { "Pending", "Accepted", "Declined", "InProgress", "Completed", "Cancelled" },
            DuelStatuses.All);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Completed")]
    public void IsValid_AcceptsKnownStatuses(string status)
    {
        Assert.True(DuelStatuses.IsValid(status));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Scheduled")] // це MatchStatus, і в дуелі його немає
    [InlineData("Postponed")]
    public void IsValid_RejectsAnythingElse(string? status)
    {
        Assert.False(DuelStatuses.IsValid(status));
    }

    [Fact]
    public void AwaitingResponse_IsOnlyPending()
    {
        Assert.True(DuelStatuses.IsAwaitingResponse(DuelStatuses.Pending));

        foreach (var status in DuelStatuses.All.Where(s => s != DuelStatuses.Pending))
        {
            Assert.False(DuelStatuses.IsAwaitingResponse(status));
        }
    }

    [Theory]
    [InlineData("Declined")]
    [InlineData("Completed")]
    [InlineData("Cancelled")]
    public void IsFinal_CoversTheStatesThatGoNowhere(string status)
    {
        Assert.True(DuelStatuses.IsFinal(status));
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Accepted")]
    [InlineData("InProgress")]
    public void IsFinal_LeavesTheLiveOnesAlone(string status)
    {
        Assert.False(DuelStatuses.IsFinal(status));
    }

    [Fact]
    public void Playable_IsAcceptedOrInProgressOnly()
    {
        // Грати можна лише після згоди й доти, доки дуель не закрито.
        Assert.True(DuelStatuses.IsPlayable(DuelStatuses.Accepted));
        Assert.True(DuelStatuses.IsPlayable(DuelStatuses.InProgress));

        Assert.False(DuelStatuses.IsPlayable(DuelStatuses.Pending));
        Assert.False(DuelStatuses.IsPlayable(DuelStatuses.Declined));
        Assert.False(DuelStatuses.IsPlayable(DuelStatuses.Completed));
        Assert.False(DuelStatuses.IsPlayable(DuelStatuses.Cancelled));
    }

    [Fact]
    public void FinalAndPlayable_NeverOverlap()
    {
        foreach (var status in DuelStatuses.All)
        {
            Assert.False(DuelStatuses.IsFinal(status) && DuelStatuses.IsPlayable(status));
        }
    }
}

/// <summary>
/// Права на дуель. Найважливіше — що ініціатор не приймає власний виклик:
/// інакше згоди другої сторони просто не існувало б.
/// </summary>
public class DuelPolicyTests
{
    private const int Challenger = 10;
    private const int Opponent = 20;
    private const int Stranger = 30;
    private const int Admin = 40;

    private static DuelPolicy.Context At(string status) => new(status, Challenger, Opponent);

    /// <summary>Відкритий виклик: суперника ще не названо.</summary>
    private static DuelPolicy.Context Open(string status) => new(status, Challenger, null);

    // ---------- Відповідь на виклик ----------

    [Fact]
    public void OnlyTheChallengedPlayerMayRespond()
    {
        var pending = At(DuelStatuses.Pending);

        Assert.True(DuelPolicy.CanRespond(pending, Opponent, isAdmin: false));
        Assert.False(DuelPolicy.CanRespond(pending, Challenger, isAdmin: false));
        Assert.False(DuelPolicy.CanRespond(pending, Stranger, isAdmin: false));
    }

    [Theory]
    [InlineData("Accepted")]
    [InlineData("Declined")]
    [InlineData("Completed")]
    [InlineData("Cancelled")]
    public void RespondingTwiceIsNotPossible(string status)
    {
        Assert.False(DuelPolicy.CanRespond(At(status), Opponent, isAdmin: false));
    }

    // ---------- Скасування ----------

    [Fact]
    public void OnlyTheInitiatorMayCancel_AndOnlyWhilePending()
    {
        Assert.True(DuelPolicy.CanCancel(At(DuelStatuses.Pending), Challenger, isAdmin: false));
        Assert.False(DuelPolicy.CanCancel(At(DuelStatuses.Pending), Opponent, isAdmin: false));

        // Після згоди дуель належить обом — зникати вона має через завершення.
        Assert.False(DuelPolicy.CanCancel(At(DuelStatuses.Accepted), Challenger, isAdmin: false));
    }

    // ---------- Ведення ----------

    [Theory]
    [InlineData(Challenger)]
    [InlineData(Opponent)]
    public void BothParticipantsMayRunAnAcceptedDuel(int userId)
    {
        // Організатора, який зробив би це за них, у дуелі немає.
        Assert.True(DuelPolicy.CanManage(At(DuelStatuses.Accepted), userId, isAdmin: false));
        Assert.True(DuelPolicy.CanManage(At(DuelStatuses.InProgress), userId, isAdmin: false));
    }

    [Fact]
    public void StrangerMayNotRunIt()
    {
        Assert.False(DuelPolicy.CanManage(At(DuelStatuses.Accepted), Stranger, isAdmin: false));
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Declined")]
    [InlineData("Completed")]
    [InlineData("Cancelled")]
    public void ADuelThatIsNotPlayableCannotBeRun(string status)
    {
        Assert.False(DuelPolicy.CanManage(At(status), Challenger, isAdmin: false));
    }

    // ---------- Відкритий виклик ----------

    [Fact]
    public void OpenChallenge_IsRecognisedByTheMissingOpponent()
    {
        Assert.True(DuelPolicy.IsOpen(Open(DuelStatuses.Pending)));
        Assert.False(DuelPolicy.IsOpen(At(DuelStatuses.Pending)));
    }

    [Theory]
    [InlineData(Opponent)]
    [InlineData(Stranger)]
    public void OpenChallenge_MayBeAcceptedByAnyoneElse(int userId)
    {
        Assert.True(DuelPolicy.CanRespond(Open(DuelStatuses.Pending), userId, isAdmin: false));
    }

    [Fact]
    public void OpenChallenge_CannotBeAcceptedByItsAuthor()
    {
        // Навіть адміністратором: згода другої сторони — це не питання прав.
        Assert.False(DuelPolicy.CanRespond(Open(DuelStatuses.Pending), Challenger, isAdmin: false));
        Assert.False(DuelPolicy.CanRespond(Open(DuelStatuses.Pending), Challenger, isAdmin: true));
    }

    [Theory]
    [InlineData("Accepted")]
    [InlineData("Cancelled")]
    public void OpenChallenge_ClosesLikeAnyOther(string status)
    {
        Assert.False(DuelPolicy.CanRespond(Open(status), Stranger, isAdmin: false));
    }

    [Fact]
    public void OpenChallenge_IsStillCancelledOnlyByItsAuthor()
    {
        Assert.True(DuelPolicy.CanCancel(Open(DuelStatuses.Pending), Challenger, isAdmin: false));
        Assert.False(DuelPolicy.CanCancel(Open(DuelStatuses.Pending), Stranger, isAdmin: false));
    }

    [Fact]
    public void OpenChallenge_HasNoSecondParticipantYet()
    {
        var open = Open(DuelStatuses.Pending);

        Assert.True(DuelPolicy.IsParticipant(open, Challenger));
        Assert.False(DuelPolicy.IsParticipant(open, Stranger));
    }

    // ---------- Адміністратор ----------

    [Fact]
    public void AdminOverridesEveryOwnershipRule()
    {
        Assert.True(DuelPolicy.CanRespond(At(DuelStatuses.Pending), Admin, isAdmin: true));
        Assert.True(DuelPolicy.CanCancel(At(DuelStatuses.Pending), Admin, isAdmin: true));
        Assert.True(DuelPolicy.CanManage(At(DuelStatuses.Accepted), Admin, isAdmin: true));
    }

    [Fact]
    public void AdminStillCannotRunAFinishedDuel()
    {
        // Стан — не питання прав: завершену дуель не веде ніхто.
        Assert.False(DuelPolicy.CanManage(At(DuelStatuses.Completed), Admin, isAdmin: true));
    }
}

/// <summary>
/// Рахунок у дуелях. Це окремий показник — у Player.TotalMatches він не
/// потрапляє й потрапити не повинен.
/// </summary>
public class DuelRecordCalculatorTests
{
    private const int Me = 1;
    private const int Rival = 2;

    private static Duel Completed(int challenger, int opponent, int? winner) =>
        new()
        {
            ChallengerPlayerId = challenger,
            OpponentPlayerId = opponent,
            WinnerPlayerId = winner,
            Status = DuelStatuses.Completed
        };

    [Fact]
    public void CountsWinsFromEitherSideOfTheDuel()
    {
        var duels = new[]
        {
            Completed(Me, Rival, Me),    // виграв як ініціатор
            Completed(Rival, Me, Me),    // виграв як викликаний
            Completed(Me, Rival, Rival)  // програв
        };

        var record = DuelRecordCalculator.Calculate(duels, Me);

        Assert.Equal(3, record.Played);
        Assert.Equal(2, record.Wins);
        Assert.Equal(1, record.Losses);
        Assert.Equal(0, record.Draws);
    }

    [Fact]
    public void ANullWinnerIsADraw_NotALoss()
    {
        var record = DuelRecordCalculator.Calculate(
            new[] { Completed(Me, Rival, Me), Completed(Me, Rival, null) }, Me);

        Assert.Equal(2, record.Played);
        Assert.Equal(1, record.Wins);
        Assert.Equal(1, record.Draws);
        Assert.Equal(0, record.Losses);

        // Нічия лишається в знаменнику — інакше один виграш із двох дав би 100%.
        Assert.Equal(50.0m, record.WinRate);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Accepted")]
    [InlineData("Declined")]
    [InlineData("InProgress")]
    [InlineData("Cancelled")]
    public void OnlyCompletedDuelsCount(string status)
    {
        var duel = Completed(Me, Rival, Me);
        duel.Status = status;

        var record = DuelRecordCalculator.Calculate(new[] { duel }, Me);

        Assert.Equal(0, record.Played);
        Assert.Equal(0, record.Wins);
    }

    [Fact]
    public void SomebodyElsesDuelsAreNotMine()
    {
        var record = DuelRecordCalculator.Calculate(
            new[] { Completed(Rival, 99, Rival) }, Me);

        Assert.Equal(0, record.Played);
    }

    [Fact]
    public void AnOpenChallengeCountsForNobody()
    {
        // Ані ініціаторові, ані комусь іще: суперника немає, і статусу
        // Completed відкритий виклик досягти не встиг.
        var open = new Duel
        {
            ChallengerPlayerId = Me,
            OpponentPlayerId = null,
            Status = DuelStatuses.Pending
        };

        Assert.Equal(0, DuelRecordCalculator.Calculate(new[] { open }, Me).Played);
    }

    [Fact]
    public void NoDuelsIsZero_NotADivisionByZero()
    {
        var record = DuelRecordCalculator.Calculate(Array.Empty<Duel>(), Me);

        Assert.Equal(0, record.Played);
        Assert.Equal(0m, record.WinRate);
    }

    [Fact]
    public void WinRateRoundsToOneDecimal()
    {
        // 1 з 3 = 33.333… → 33.3, як у PlayerRecordCalculator.
        var duels = new[]
        {
            Completed(Me, Rival, Me),
            Completed(Me, Rival, Rival),
            Completed(Me, Rival, Rival)
        };

        Assert.Equal(33.3m, DuelRecordCalculator.Calculate(duels, Me).WinRate);
    }
}
