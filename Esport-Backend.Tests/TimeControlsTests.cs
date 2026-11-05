using TForge.Common;

namespace TForge.Tests;

public class TimeControlsTests
{
    [Theory]
    [InlineData(1, 0, TimeControls.Bullet)]       // 60 s
    [InlineData(2, 1, TimeControls.Bullet)]       // 160 s
    [InlineData(3, 0, TimeControls.Blitz)]        // 180 s — межа належить Blitz
    [InlineData(5, 3, TimeControls.Blitz)]        // 420 s
    [InlineData(8, 0, TimeControls.Rapid)]        // 480 s
    [InlineData(15, 10, TimeControls.Rapid)]      // 1300 s
    [InlineData(25, 0, TimeControls.Classical)]   // 1500 s
    [InlineData(30, 20, TimeControls.Classical)]
    public void FromClock_UsesLichessThresholds(double baseMinutes, double increment, string expected) =>
        Assert.Equal(expected, TimeControls.FromClock(baseMinutes, increment));

    [Fact]
    public void Default_IsRapid() => Assert.Equal(TimeControls.Rapid, TimeControls.Default);

    [Theory]
    [InlineData("Blitz", true)]
    [InlineData("blitz", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_IsExact(string? value, bool expected) =>
        Assert.Equal(expected, TimeControls.IsValid(value));
}
