using TForge.Common;
using TForge.DTOs;
using TForge.Validators;
using Xunit;

namespace TForge.Tests;

/// <summary>
/// Три сутності планують гру — матч, виклик команді та дуель — і всі три
/// мусять відмовляти минулому часу однаково. Раніше вони розходилися: дуель
/// не перевіряла нічого, а матч порівнював UTC з місцевим часом.
/// </summary>
public class SchedulingValidatorsTests
{
    private static readonly DateTime Future = DateTime.Now.AddDays(1);
    private static readonly DateTime Past = DateTime.Now.AddDays(-1);

    private static CreateMatchDto Match(DateTime scheduledAt) => new()
    {
        Game = Games.Valorant,
        ScheduledAt = scheduledAt,
        MatchType = MatchTypes.GroupStage,
        Format = "BO1"
    };

    private static CreateMatchChallengeDto Challenge(DateTime proposedAt) => new()
    {
        ChallengerTeamId = 1,
        Game = Games.Valorant,
        ProposedAt = proposedAt,
        Format = "BO1"
    };

    private static CreateDuelDto Duel(DateTime scheduledAt) => new()
    {
        OpponentPlayerId = 2,
        Game = Games.Valorant,
        ScheduledAt = scheduledAt,
        Format = "BO1"
    };

    [Fact]
    public void Match_InThePast_IsRejected()
    {
        Assert.False(new CreateMatchValidator().Validate(Match(Past)).IsValid);
    }

    [Fact]
    public void Match_InTheFuture_IsAccepted()
    {
        Assert.True(new CreateMatchValidator().Validate(Match(Future)).IsValid);
    }

    // Найближчі години теж мусять проходити: саме їх відсікало порівняння з
    // DateTime.Now, і на машині в UTC+3 матч на сьогоднішній вечір створити
    // було не можна.
    [Fact]
    public void Match_LaterToday_IsAccepted()
    {
        Assert.True(new CreateMatchValidator().Validate(Match(DateTime.Now.AddMinutes(30))).IsValid);
    }

    [Fact]
    public void Challenge_InThePast_IsRejected()
    {
        Assert.False(new CreateMatchChallengeValidator().Validate(Challenge(Past)).IsValid);
    }

    [Fact]
    public void Challenge_InTheFuture_IsAccepted()
    {
        Assert.True(new CreateMatchChallengeValidator().Validate(Challenge(Future)).IsValid);
    }

    [Fact]
    public void Duel_InThePast_IsRejected()
    {
        Assert.False(new CreateDuelValidator().Validate(Duel(Past)).IsValid);
    }

    [Fact]
    public void Duel_InTheFuture_IsAccepted()
    {
        Assert.True(new CreateDuelValidator().Validate(Duel(Future)).IsValid);
    }

    // Відкритий виклик іде без суперника — і це не привід відхилити його.
    [Fact]
    public void Duel_WithoutOpponent_IsAccepted()
    {
        var duel = Duel(Future);
        duel.OpponentPlayerId = null;

        Assert.True(new CreateDuelValidator().Validate(duel).IsValid);
    }

    [Fact]
    public void Duel_UnknownGame_IsRejected()
    {
        var duel = Duel(Future);
        duel.Game = "Fortnite";

        Assert.False(new CreateDuelValidator().Validate(duel).IsValid);
    }
}
