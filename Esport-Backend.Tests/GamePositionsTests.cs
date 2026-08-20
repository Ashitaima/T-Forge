using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class GamePositionsTests
{
    [Fact]
    public void Valorant_HasItsOwnRoles()
    {
        Assert.Equal(new[] { "Duelist", "Initiator", "Sentinel", "Controller" },
            GamePositions.For(Games.Valorant));
    }

    [Fact]
    public void UnknownGame_HasNoPositions()
    {
        Assert.Empty(GamePositions.For("Minesweeper"));
    }

    [Fact]
    public void EveryGame_HasPositions()
    {
        Assert.All(Games.All, game => Assert.NotEmpty(GamePositions.For(game)));
    }

    // Саме заради цього перелік розібрано за дисциплінами: «AWPer» — правильна
    // позиція, але не у Valorant.
    [Fact]
    public void PositionFromAnotherGame_IsRejected()
    {
        Assert.False(GamePositions.IsValidFor(Games.Valorant, "AWPer"));
        Assert.True(GamePositions.IsValidFor(Games.CS2, "AWPer"));
    }

    [Fact]
    public void EmptyPosition_IsAllowed()
    {
        Assert.True(GamePositions.IsValidFor(Games.CS2, ""));
        Assert.True(GamePositions.IsValidFor(Games.CS2, null));
    }

    [Fact]
    public void PositionForUnknownGame_IsRejected()
    {
        Assert.False(GamePositions.IsValidFor("Minesweeper", "Rifler"));
    }

    // «Support» існує і в Dota 2, і в League of Legends, але це різні ролі —
    // тому переліки й не спільні.
    [Fact]
    public void SupportIsNotShared_BetweenMobas()
    {
        Assert.Contains("Support", GamePositions.For(Games.LeagueOfLegends));
        Assert.DoesNotContain("Support", GamePositions.For(Games.Dota2));
    }
}
