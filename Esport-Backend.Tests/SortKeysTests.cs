using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class SortKeysTests
{
    [Theory]
    [InlineData("nickname")]
    [InlineData("position")]
    [InlineData("country")]
    [InlineData("team")]
    [InlineData("matches")]
    [InlineData("wins")]
    [InlineData("winRate")]
    [InlineData("kda")]
    [InlineData("rating")]
    public void PlayerSortKeys_Known_IsValid(string key)
    {
        Assert.True(PlayerSortKeys.IsValid(key));
    }

    [Theory]
    [InlineData("passwordHash")]
    [InlineData("Nickname")]
    [InlineData("")]
    [InlineData(null)]
    public void PlayerSortKeys_Unknown_IsNotValid(string? key)
    {
        Assert.False(PlayerSortKeys.IsValid(key));
    }

    [Theory]
    [InlineData("name")]
    [InlineData("tag")]
    [InlineData("region")]
    [InlineData("played")]
    [InlineData("wins")]
    [InlineData("winRate")]
    [InlineData("titles")]
    [InlineData("rating")]
    public void TeamSortKeys_Known_IsValid(string key)
    {
        Assert.True(TeamSortKeys.IsValid(key));
    }

    [Theory]
    [InlineData("captainId")]
    [InlineData("Name")]
    [InlineData(null)]
    public void TeamSortKeys_Unknown_IsNotValid(string? key)
    {
        Assert.False(TeamSortKeys.IsValid(key));
    }

    // Ключі йдуть у URL і в атрибути кнопок — пробіл чи велика літера
    // означали б розходження між фронтендом і бекендом.
    [Fact]
    public void AllKeys_AreLowerCamelCaseWithoutSpaces()
    {
        foreach (var key in PlayerSortKeys.All.Concat(TeamSortKeys.All))
        {
            Assert.False(string.IsNullOrWhiteSpace(key));
            Assert.DoesNotContain(" ", key);
            Assert.True(char.IsLower(key[0]), $"Ключ '{key}' має починатися з малої літери");
        }
    }

    [Fact]
    public void AllKeys_HaveNoDuplicates()
    {
        Assert.Equal(PlayerSortKeys.All.Length, PlayerSortKeys.All.Distinct().Count());
        Assert.Equal(TeamSortKeys.All.Length, TeamSortKeys.All.Distinct().Count());
    }
}
