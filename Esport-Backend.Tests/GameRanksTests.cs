using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class GameRanksTests
{
    [Fact]
    public void EveryGame_HasRanks() =>
        Assert.All(Games.All, game => Assert.NotEmpty(GameRanks.For(game)));

    [Fact]
    public void UnknownGame_HasNoRanks() =>
        Assert.Empty(GameRanks.For("Minesweeper"));

    [Fact]
    public void Ranks_AreDistinctWithinAGame() =>
        Assert.All(Games.All, game =>
        {
            var ranks = GameRanks.For(game);
            Assert.Equal(ranks.Length, ranks.Distinct().Count());
        });

    [Theory]
    [InlineData(Games.Valorant, 25)]
    [InlineData(Games.Dota2, 36)]
    [InlineData(Games.LeagueOfLegends, 31)]
    [InlineData(Games.CS2, 7)]
    [InlineData(Games.Chess, 9)]
    public void Ladder_HasTheExpectedSize(string game, int expected) =>
        Assert.Equal(expected, GameRanks.For(game).Length);

    // ---- IsValidFor ----

    [Fact]
    public void EmptyRank_IsValid() =>
        // «Не вказав» — не помилка, як і порожня позиція.
        Assert.True(GameRanks.IsValidFor(Games.Valorant, ""));

    [Fact]
    public void RankOfAnotherGame_IsNotValid() =>
        // Radiant існує, але не в CS2 — саме та плутанина, задля якої
        // ранги розібрані за дисциплінами.
        Assert.False(GameRanks.IsValidFor(Games.CS2, "Radiant"));

    [Fact]
    public void RankOfTheSameGame_IsValid() =>
        Assert.True(GameRanks.IsValidFor(Games.Valorant, "Ascendant2"));

    // ---- AtOrAbove ----

    [Fact]
    public void AtOrAbove_BottomOfTheLadder_ReturnsEveryRank() =>
        Assert.Equal(GameRanks.For(Games.Valorant), GameRanks.AtOrAbove(Games.Valorant, "Iron1"));

    [Fact]
    public void AtOrAbove_TopOfTheLadder_ReturnsOnlyIt() =>
        Assert.Equal(new[] { "Radiant" }, GameRanks.AtOrAbove(Games.Valorant, "Radiant"));

    [Fact]
    public void AtOrAbove_IncludesTheFloorItself() =>
        Assert.Contains("Ascendant2", GameRanks.AtOrAbove(Games.Valorant, "Ascendant2"));

    [Fact]
    public void AtOrAbove_ExcludesEverythingBelowTheFloor() =>
        Assert.DoesNotContain("Ascendant1", GameRanks.AtOrAbove(Games.Valorant, "Ascendant2"));

    [Fact]
    public void AtOrAbove_NoFloor_AlsoAdmitsPlayersWithoutARank() =>
        // Вакансія без межі мусить бачити й того, хто рангу не вказав, —
        // інакше порожнє поле тихо виключало б гравця з усіх стрічок.
        Assert.Contains("", GameRanks.AtOrAbove(Games.Valorant, ""));

    [Fact]
    public void AtOrAbove_WithAFloor_ExcludesPlayersWithoutARank() =>
        Assert.DoesNotContain("", GameRanks.AtOrAbove(Games.Valorant, "Ascendant2"));

    [Fact]
    public void AtOrAbove_UnknownFloor_ReturnsNothing() =>
        // Невідомий ранг — не «будь-який»: інакше зіпсоване значення
        // мовчки відкрило б вакансію всім.
        Assert.Empty(GameRanks.AtOrAbove(Games.Valorant, "Bogus"));

    [Fact]
    public void Chess_BandsAreOrderedByLowerBound() =>
        Assert.Equal(
            new[] { 0, 1000, 1200, 1400, 1600, 1800, 2000, 2200, 2400 },
            GameRanks.Chess.Select(b => int.Parse(b["Chess".Length..])).ToArray());

    [Fact]
    public void Chess_AtOrAbove_1800_ReturnsTheTopFourBands() =>
        Assert.Equal(
            new[] { "Chess1800", "Chess2000", "Chess2200", "Chess2400" },
            GameRanks.AtOrAbove(Games.Chess, "Chess1800"));
}
