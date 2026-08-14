using TForge.Common;
using TForge.Models;
using Xunit;

namespace TForge.Tests;

public class PlayerRecordCalculatorTests
{
    private const int OldTeam = 1;
    private const int NewTeam = 2;
    private const int Rival = 3;

    private static MatchPlayer Row(int playedForTeamId, int winnerTeamId, int kills = 0, int deaths = 0, int assists = 0) =>
        new()
        {
            TeamId = playedForTeamId,
            Kills = kills,
            Deaths = deaths,
            Assists = assists,
            Match = new Match
            {
                HomeTeamId = playedForTeamId,
                AwayTeamId = Rival,
                Status = MatchStatus.Completed,
                WinnerTeamId = winnerTeamId
            }
        };

    [Fact]
    public void Calculate_CreditsWin_WhenRowTeamMatchesWinner()
    {
        var record = PlayerRecordCalculator.Calculate(new[] { Row(OldTeam, OldTeam) });

        Assert.Equal(1, record.Matches);
        Assert.Equal(1, record.Wins);
        Assert.Equal(0, record.Losses);
    }

    [Fact]
    public void Calculate_CreditsLoss_WhenRowTeamIsNotWinner()
    {
        var record = PlayerRecordCalculator.Calculate(new[] { Row(OldTeam, Rival) });

        Assert.Equal(0, record.Wins);
        Assert.Equal(1, record.Losses);
    }

    [Fact]
    public void Calculate_TransferredPlayer_CountsMatchForTeamOnTheRow()
    {
        // Гравець зараз у NewTeam, але цей матч грав за OldTeam і виграв його
        var record = PlayerRecordCalculator.Calculate(new[]
        {
            Row(OldTeam, OldTeam),
            Row(NewTeam, Rival)
        });

        Assert.Equal(2, record.Matches);
        Assert.Equal(1, record.Wins);
        Assert.Equal(1, record.Losses);
    }

    [Fact]
    public void Calculate_IgnoresMatchesThatAreNotCompleted()
    {
        var scheduled = Row(OldTeam, OldTeam);
        scheduled.Match.Status = MatchStatus.Scheduled;
        scheduled.Match.WinnerTeamId = null;

        var record = PlayerRecordCalculator.Calculate(new[] { Row(OldTeam, OldTeam), scheduled });

        Assert.Equal(1, record.Matches);
    }

    [Fact]
    public void Calculate_SumsCombatStatistics()
    {
        var record = PlayerRecordCalculator.Calculate(new[]
        {
            Row(OldTeam, OldTeam, kills: 20, deaths: 10, assists: 5),
            Row(OldTeam, Rival, kills: 10, deaths: 10, assists: 5)
        });

        Assert.Equal(30, record.Kills);
        Assert.Equal(20, record.Deaths);
        Assert.Equal(10, record.Assists);
        Assert.Equal(2.0, record.Kda);
    }

    [Fact]
    public void Calculate_ZeroDeaths_TreatedAsOne()
    {
        var record = PlayerRecordCalculator.Calculate(new[]
        {
            Row(OldTeam, OldTeam, kills: 3, deaths: 0, assists: 1)
        });

        Assert.Equal(4.0, record.Kda);
    }

    [Fact]
    public void Calculate_NoRows_ReturnsZeroes()
    {
        var record = PlayerRecordCalculator.Calculate(Array.Empty<MatchPlayer>());

        Assert.Equal(0, record.Matches);
        Assert.Equal(0m, record.WinRate);
        Assert.Equal(0.0, record.Kda);
    }
}
