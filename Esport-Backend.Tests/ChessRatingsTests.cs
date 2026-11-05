using TForge.Common;

namespace TForge.Tests;

public class ChessRatingsTests
{
    [Theory]
    [InlineData(100, "Chess0")]
    [InlineData(999, "Chess0")]
    [InlineData(1000, "Chess1000")]
    [InlineData(1199, "Chess1000")]
    [InlineData(1850, "Chess1800")]
    [InlineData(2400, "Chess2400")]
    [InlineData(3500, "Chess2400")]
    public void BandFor_PicksTheHighestBandNotAboveTheRating(int rating, string expected) =>
        Assert.Equal(expected, ChessRatings.BandFor(rating));

    [Fact]
    public void BandFor_EveryResultIsACatalogValue() =>
        Assert.All(Enumerable.Range(ChessRatings.Min, ChessRatings.Max - ChessRatings.Min + 1),
            r => Assert.Contains(ChessRatings.BandFor(r), GameRanks.Chess));

    [Theory]
    [InlineData("Chess0", 900)]
    [InlineData("Chess1000", 1100)]
    [InlineData("Chess2200", 2300)]
    [InlineData("Chess2400", 2500)]
    [InlineData("", null)]
    [InlineData(null, null)]
    [InlineData("Radiant", null)]
    public void Midpoint_OfABand(string? band, int? expected) =>
        Assert.Equal(expected, ChessRatings.Midpoint(band));

    [Fact]
    public void Effective_PrefersTheExactRating() =>
        Assert.Equal(1777, ChessRatings.Effective(1777, "Chess1000"));

    [Fact]
    public void Effective_FallsBackToTheBandMidpoint() =>
        Assert.Equal(1500, ChessRatings.Effective(null, "Chess1400"));

    [Fact]
    public void Effective_NothingKnown_IsNull() =>
        Assert.Null(ChessRatings.Effective(null, ""));
}
