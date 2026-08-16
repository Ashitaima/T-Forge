using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class GamesTests
{
    [Theory]
    [InlineData("CS2")]
    [InlineData("Valorant")]
    [InlineData("Dota2")]
    [InlineData("LeagueOfLegends")]
    public void IsValid_KnownGame_IsTrue(string game)
    {
        Assert.True(Games.IsValid(game));
    }

    [Theory]
    [InlineData("Overwatch")]
    [InlineData("cs2")]
    [InlineData("Dota 2")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_UnknownGame_IsFalse(string? game)
    {
        Assert.False(Games.IsValid(game));
    }

    [Fact]
    public void All_HasNoDuplicates()
    {
        Assert.Equal(Games.All.Length, Games.All.Distinct().Count());
    }

    // Колонки tournaments."Game" і matches."Game" обмежені 50 символами.
    [Fact]
    public void All_FitTheColumn()
    {
        Assert.All(Games.All, game => Assert.True(game.Length <= 50));
    }
}
