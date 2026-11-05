using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class GameHeroesTests
{
    [Theory]
    [InlineData(Games.Dota2)]
    [InlineData(Games.LeagueOfLegends)]
    public void ForGame_MobaCatalogIsPlausiblySized(string game)
    {
        // Точна кількість змінюється з кожним новим героєм, тож закріплюємо
        // межі, а не число: провалиться порожній чи обрізаний каталог.
        Assert.InRange(GameHeroes.ForGame(game).Length, 100, 400);
    }

    [Theory]
    [InlineData(Games.CS2)]
    [InlineData(Games.Valorant)]
    [InlineData("Overwatch")]
    [InlineData(null)]
    public void ForGame_NoHeroes_IsEmpty(string? game) => Assert.Empty(GameHeroes.ForGame(game));

    [Theory]
    [InlineData(Games.Dota2)]
    [InlineData(Games.LeagueOfLegends)]
    public void ForGame_SlugsAreUnique(string game) =>
        Assert.Equal(GameHeroes.ForGame(game).Length, GameHeroes.ForGame(game).Distinct().Count());

    [Theory]
    [InlineData(Games.Dota2)]
    [InlineData(Games.LeagueOfLegends)]
    public void ForGame_NoSlugIsBlank(string game) =>
        Assert.All(GameHeroes.ForGame(game), slug => Assert.False(string.IsNullOrWhiteSpace(slug)));

    [Theory]
    [InlineData(Games.Dota2)]
    [InlineData(Games.LeagueOfLegends)]
    public void ForGame_SlugsAreUrlSafe(string game) =>
        // Слаг іде просто в URL портрета, тож пробіл чи апостроф там означав би
        // биту картинку. Саме тому каталог генерується, а не пишеться руками.
        Assert.All(
            GameHeroes.ForGame(game),
            slug => Assert.Matches("^[A-Za-z0-9_]+$", slug));

    [Fact]
    public void IsValid_HeroOfAnotherGame_IsFalse()
    {
        var dotaHero = GameHeroes.Dota2[0];
        Assert.False(GameHeroes.IsValid(Games.LeagueOfLegends, dotaHero));
    }

    [Fact]
    public void IsValid_OwnHero_IsTrue() => Assert.True(GameHeroes.IsValid(Games.Dota2, GameHeroes.Dota2[0]));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_Blank_IsFalse(string? hero) => Assert.False(GameHeroes.IsValid(Games.Dota2, hero));

    [Fact]
    public void DdragonVersion_IsSet() => Assert.False(string.IsNullOrWhiteSpace(GameHeroes.DdragonVersion));

    [Fact]
    public void HasHeroes_Moba_IsTrue() => Assert.True(GameHeroes.HasHeroes(Games.Dota2));

    [Fact]
    public void HasHeroes_Shooter_IsFalse() => Assert.False(GameHeroes.HasHeroes(Games.CS2));

    [Fact]
    public void EveryGame_HasEitherMapsOrHeroes_NeverBoth() =>
        // Кожна дисципліна драфтить рівно одне: або карти, або героїв. На цьому
        // тримається DraftEngine.CatalogFor, який обирає між ними саме так.
        Assert.All(
            // Шахи драфту не мають зовсім — ні карт, ні героїв, і це навмисно.
            Games.All.Where(DraftSequences.HasDraft),
            game => Assert.True(GameMaps.HasMaps(game) ^ GameHeroes.HasHeroes(game)));
}
