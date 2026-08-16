using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class PlayerPositionsTests
{
    [Theory]
    [InlineData("Support")]
    [InlineData("AWPer")]
    [InlineData("Jungle")]
    public void IsValid_KnownPosition_IsTrue(string position)
    {
        Assert.True(PlayerPositions.IsValid(position));
    }

    [Theory]
    [InlineData("Carry")]
    [InlineData("support")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_UnknownPosition_IsFalse(string? position)
    {
        Assert.False(PlayerPositions.IsValid(position));
    }

    // Фронтенд показує саме цей список, тож дублікати означали б
    // повторений пункт у випадному списку.
    [Fact]
    public void All_HasNoDuplicates()
    {
        Assert.Equal(PlayerPositions.All.Length, PlayerPositions.All.Distinct().Count());
    }

    // Колонка players.Position обмежена 50 символами.
    [Fact]
    public void All_FitTheColumn()
    {
        Assert.All(PlayerPositions.All, position => Assert.True(position.Length <= 50));
    }
}
