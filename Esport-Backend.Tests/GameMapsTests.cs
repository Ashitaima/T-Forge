using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class GameMapsTests
{
    [Theory]
    [InlineData(Games.CS2)]
    [InlineData(Games.Valorant)]
    public void ForGame_ShooterHasSevenMaps(string game) => Assert.Equal(7, GameMaps.ForGame(game).Length);

    [Theory]
    [InlineData(Games.Dota2)]
    [InlineData(Games.LeagueOfLegends)]
    [InlineData("Overwatch")]
    [InlineData(null)]
    public void ForGame_NoMaps_IsEmpty(string? game) => Assert.Empty(GameMaps.ForGame(game));

    [Theory]
    [InlineData(Games.CS2)]
    [InlineData(Games.Valorant)]
    public void HasMaps_Shooter_IsTrue(string game) => Assert.True(GameMaps.HasMaps(game));

    [Fact]
    public void HasMaps_Moba_IsFalse() => Assert.False(GameMaps.HasMaps(Games.Dota2));

    [Theory]
    [InlineData(Games.CS2)]
    [InlineData(Games.Valorant)]
    public void ForGame_HasNoDuplicates(string game) =>
        Assert.Equal(GameMaps.ForGame(game).Length, GameMaps.ForGame(game).Distinct().Count());

    [Theory]
    [InlineData(Games.CS2)]
    [InlineData(Games.Valorant)]
    public void ForGame_NoMapIsBlank(string game) =>
        Assert.All(GameMaps.ForGame(game), map => Assert.False(string.IsNullOrWhiteSpace(map)));

    [Fact]
    public void IsValid_MapOfThatGame_IsTrue() => Assert.True(GameMaps.IsValid(Games.CS2, "Mirage"));

    [Fact]
    public void IsValid_MapOfAnotherGame_IsFalse() => Assert.False(GameMaps.IsValid(Games.CS2, "Ascent"));

    [Theory]
    [InlineData("mirage")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_Junk_IsFalse(string? map) => Assert.False(GameMaps.IsValid(Games.CS2, map));

    // ---- Сторони ----

    [Theory]
    [InlineData(Games.CS2)]
    [InlineData(Games.Valorant)]
    [InlineData(Games.Dota2)]
    [InlineData(Games.LeagueOfLegends)]
    public void Sides_EveryGameHasExactlyTwo(string game) => Assert.Equal(2, GameSides.ForGame(game).Length);

    [Fact]
    public void Sides_Unknown_IsEmpty() => Assert.Empty(GameSides.ForGame("Overwatch"));

    [Fact]
    public void Sides_IsValid_KnownSide_IsTrue() => Assert.True(GameSides.IsValid(Games.Dota2, "Radiant"));

    [Fact]
    public void Sides_IsValid_SideOfAnotherGame_IsFalse() => Assert.False(GameSides.IsValid(Games.Dota2, "CT"));

    [Fact]
    public void Sides_EveryKnownGameHasSides() =>
        Assert.All(Games.All, game => Assert.NotEmpty(GameSides.ForGame(game)));

    [Fact]
    public void Chess_SidesAreWhiteThenBlack() =>
        Assert.Equal(new[] { "White", "Black" }, GameSides.ForGame(Games.Chess));
}
