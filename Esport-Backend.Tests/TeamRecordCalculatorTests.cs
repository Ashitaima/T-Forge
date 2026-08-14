using TForge.Common;
using TForge.Models;
using Xunit;

namespace TForge.Tests;

public class TeamRecordCalculatorTests
{
    private const int Us = 1;
    private const int Them = 2;

    /// <summary>Матч нашої команди: results — хто виграв, або null якщо не завершено.</summary>
    private static Match Played(int day, int? winnerTeamId, string status = MatchStatus.Completed) =>
        new()
        {
            Id = day,
            HomeTeamId = Us,
            AwayTeamId = Them,
            ScheduledAt = new DateTime(2026, 1, day, 12, 0, 0, DateTimeKind.Utc),
            Status = status,
            WinnerTeamId = winnerTeamId
        };

    [Fact]
    public void CalculateStreak_NoMatches_ReturnsNull()
    {
        Assert.Null(TeamRecordCalculator.CalculateStreak(Array.Empty<Match>(), Us));
    }

    [Fact]
    public void CalculateStreak_OnlyScheduledMatches_ReturnsNull()
    {
        var matches = new[] { Played(1, null, MatchStatus.Scheduled) };
        Assert.Null(TeamRecordCalculator.CalculateStreak(matches, Us));
    }

    [Fact]
    public void CalculateStreak_AllWins_CountsEveryMatch()
    {
        var matches = new[] { Played(1, Us), Played(2, Us), Played(3, Us) };
        var streak = TeamRecordCalculator.CalculateStreak(matches, Us);

        Assert.NotNull(streak);
        Assert.Equal(ResultType.Win, streak!.Type);
        Assert.Equal(3, streak.Count);
    }

    [Fact]
    public void CalculateStreak_AllLosses_ReportsLossType()
    {
        var matches = new[] { Played(1, Them), Played(2, Them) };
        var streak = TeamRecordCalculator.CalculateStreak(matches, Us);

        Assert.Equal(ResultType.Loss, streak!.Type);
        Assert.Equal(2, streak.Count);
    }

    [Fact]
    public void CalculateStreak_AlternatingResults_CountsOne()
    {
        var matches = new[] { Played(1, Us), Played(2, Them), Played(3, Us) };
        var streak = TeamRecordCalculator.CalculateStreak(matches, Us);

        Assert.Equal(ResultType.Win, streak!.Type);
        Assert.Equal(1, streak.Count);
    }

    [Fact]
    public void CalculateStreak_CancelledMatchMidRun_DoesNotBreakStreak()
    {
        var matches = new[]
        {
            Played(1, Us),
            Played(2, null, MatchStatus.Cancelled),
            Played(3, Us)
        };

        var streak = TeamRecordCalculator.CalculateStreak(matches, Us);

        Assert.Equal(ResultType.Win, streak!.Type);
        Assert.Equal(2, streak.Count);
    }

    [Fact]
    public void CalculateStreak_ScheduledMatchMidRun_DoesNotBreakStreak()
    {
        var matches = new[]
        {
            Played(1, Us),
            Played(2, null, MatchStatus.Scheduled),
            Played(3, Us)
        };

        var streak = TeamRecordCalculator.CalculateStreak(matches, Us);

        Assert.Equal(2, streak!.Count);
    }

    [Fact]
    public void CalculateStreak_MostRecentMatchDeterminesType()
    {
        // Найновіший матч (день 3) — поразка, тому серія має бути програшною
        var matches = new[] { Played(1, Us), Played(2, Us), Played(3, Them) };
        var streak = TeamRecordCalculator.CalculateStreak(matches, Us);

        Assert.Equal(ResultType.Loss, streak!.Type);
        Assert.Equal(1, streak.Count);
    }

    [Fact]
    public void CalculateRecord_CountsHomeAndAwayMatches()
    {
        var home = Played(1, Us);
        var away = new Match
        {
            Id = 2,
            HomeTeamId = Them,
            AwayTeamId = Us,
            ScheduledAt = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc),
            Status = MatchStatus.Completed,
            WinnerTeamId = Us
        };

        var record = TeamRecordCalculator.CalculateRecord(new[] { home, away }, Us);

        Assert.Equal(2, record.Played);
        Assert.Equal(2, record.Wins);
        Assert.Equal(0, record.Losses);
    }

    [Fact]
    public void CalculateRecord_ExcludesMatchesWithoutWinner()
    {
        var matches = new[]
        {
            Played(1, Us),
            Played(2, null, MatchStatus.Scheduled),
            Played(3, null, MatchStatus.Cancelled)
        };

        var record = TeamRecordCalculator.CalculateRecord(matches, Us);

        Assert.Equal(1, record.Played);
        Assert.Equal(1, record.Wins);
        Assert.Equal(0, record.Losses);
    }

    [Fact]
    public void CalculateRecord_NothingPlayed_WinRateIsZero()
    {
        var record = TeamRecordCalculator.CalculateRecord(Array.Empty<Match>(), Us);

        Assert.Equal(0, record.Played);
        Assert.Equal(0m, record.WinRate);
    }

    [Fact]
    public void CalculateRecord_WinRateRoundsToOneDecimal()
    {
        // 1 перемога з 3 матчів = 33.333… -> 33.3
        var matches = new[] { Played(1, Us), Played(2, Them), Played(3, Them) };
        var record = TeamRecordCalculator.CalculateRecord(matches, Us);

        Assert.Equal(33.3m, record.WinRate);
    }
}
