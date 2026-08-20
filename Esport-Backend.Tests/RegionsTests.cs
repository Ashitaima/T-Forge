using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class RegionsTests
{
    [Theory]
    [InlineData("Europe")]
    [InlineData("North America")]
    [InlineData("CIS")]
    [InlineData("Asia")]
    public void IsValid_KnownRegion_IsTrue(string region)
    {
        Assert.True(Regions.IsValid(region));
    }

    // Саме заради цього перелік і зʼявився: «Europe», «EU» і «Європа» були
    // трьома різними регіонами, за якими не згрупувати нічого.
    [Theory]
    [InlineData("Європа")]
    [InlineData("EU")]
    [InlineData("europe")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_FreeText_IsFalse(string? region)
    {
        Assert.False(Regions.IsValid(region));
    }

    // Значення вже лежать у базі, тож зміна перелому не потребувала міграції —
    // ці два мусять лишатися валідними.
    [Fact]
    public void SeededRegions_StayValid()
    {
        Assert.True(Regions.IsValid("Europe"));
        Assert.True(Regions.IsValid("North America"));
    }

    [Fact]
    public void All_FitTheColumn()
    {
        Assert.All(Regions.All, region => Assert.True(region.Length <= 100));
    }
}
