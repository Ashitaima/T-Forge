using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class TimeZonesTests
{
    [Fact]
    public void Offsets_AreSortedAndDistinct()
    {
        var offsets = TimeZones.AllMinutes;

        Assert.Equal(offsets.OrderBy(x => x).ToArray(), offsets);
        Assert.Equal(offsets.Length, offsets.Distinct().Count());
    }

    [Fact]
    public void Offsets_SpanTheRealRange()
    {
        Assert.Equal(-720, TimeZones.AllMinutes.First());
        Assert.Equal(840, TimeZones.AllMinutes.Last());
    }

    [Theory]
    [InlineData(0, "UTC+00:00")]
    [InlineData(120, "UTC+02:00")]
    [InlineData(330, "UTC+05:30")]   // Індія
    [InlineData(345, "UTC+05:45")]   // Непал
    [InlineData(-180, "UTC-03:00")]
    [InlineData(-570, "UTC-09:30")]  // Маркізькі острови
    public void Format_RendersHalfAndQuarterHours(int minutes, string expected) =>
        Assert.Equal(expected, TimeZones.Format(minutes));

    [Fact]
    public void IsValid_KnownOffset_Yes() => Assert.True(TimeZones.IsValid(330));

    [Fact]
    public void IsValid_UnknownOffset_No() =>
        // 07 хвилин — не зсув жодної країни; ловимо зіпсоване значення тут,
        // а не тоді, коли за ним нікого не знайдуть.
        Assert.False(TimeZones.IsValid(7));

    [Fact]
    public void IsValid_Null_Yes() =>
        // «Не вказав» — валідно, як і порожній ранг.
        Assert.True(TimeZones.IsValid(null));

    // ---- IsWithin ----

    [Theory]
    [InlineData(120, 0, 180, true)]
    [InlineData(240, 0, 180, false)]
    [InlineData(-60, 0, 180, false)]
    [InlineData(0, 0, 180, true)]      // нижня межа включна
    [InlineData(180, 0, 180, true)]    // верхня межа включна
    public void IsWithin_ClosedWindow(int offset, int min, int max, bool expected) =>
        Assert.Equal(expected, TimeZones.IsWithin(offset, min, max));

    [Fact]
    public void IsWithin_OpenBelow() => Assert.True(TimeZones.IsWithin(-600, null, 180));

    [Fact]
    public void IsWithin_OpenAbove() => Assert.True(TimeZones.IsWithin(600, 0, null));

    [Fact]
    public void IsWithin_NoWindowAtAll_AdmitsAnything() =>
        Assert.True(TimeZones.IsWithin(600, null, null));

    [Fact]
    public void IsWithin_NoOffset_AdmittedOnlyByAnUnboundedWindow()
    {
        Assert.True(TimeZones.IsWithin(null, null, null));
        Assert.False(TimeZones.IsWithin(null, 0, 180));
        Assert.False(TimeZones.IsWithin(null, 0, null));
    }
}
