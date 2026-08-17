using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class CountriesTests
{
    [Theory]
    [InlineData("UA")]
    [InlineData("US")]
    [InlineData("SE")]
    [InlineData("KR")]
    public void IsValid_KnownCode_True(string code)
    {
        Assert.True(Countries.IsValid(code));
    }

    [Theory]
    [InlineData("Ukraine")]  // назва, а не код
    [InlineData("ua")]       // регістр має значення: у базі лежить саме верхній
    [InlineData("UKR")]      // alpha-3, а не alpha-2
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_AnythingElse_False(string? code)
    {
        Assert.False(Countries.IsValid(code));
    }

    // Прапор виводиться з коду як пара regional indicator, тож рівно дві
    // великі латинські літери — це не стиль, а вимога.
    [Fact]
    public void AllCodes_AreTwoUppercaseLetters()
    {
        foreach (var code in Countries.All)
        {
            Assert.Equal(2, code.Length);
            Assert.All(code, character => Assert.True(
                character is >= 'A' and <= 'Z',
                $"Код '{code}' має складатися з великих латинських літер"));
        }
    }

    [Fact]
    public void AllCodes_HaveNoDuplicates()
    {
        Assert.Equal(Countries.All.Length, Countries.All.Distinct().Count());
    }

    // ---- Переведення старих значень ----

    [Fact]
    public void ToCode_AlreadyACode_ReturnsItUnchanged()
    {
        Assert.Equal("UA", Countries.ToCode("UA"));
    }

    [Theory]
    [InlineData("Ukraine", "UA")]
    [InlineData("USA", "US")]
    [InlineData("Canada", "CA")]
    [InlineData("Germany", "DE")]
    [InlineData("South Korea", "KR")]
    public void ToCode_LegacyName_Translates(string stored, string expected)
    {
        Assert.Equal(expected, Countries.ToCode(stored));
    }

    [Fact]
    public void ToCode_IsCaseInsensitiveForNames()
    {
        Assert.Equal("UA", Countries.ToCode("ukraine"));
    }

    [Theory]
    [InlineData("Atlantis")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ToCode_UnknownOrEmpty_ReturnsNull(string? stored)
    {
        // null означає «нема на що міняти» — чужі дані краще лишити без
        // прапора, ніж стерти.
        Assert.Null(Countries.ToCode(stored));
    }

    [Fact]
    public void LegacyNames_AllPointAtRealCodes()
    {
        foreach (var (name, code) in Countries.LegacyNames)
        {
            Assert.True(Countries.IsValid(code), $"Назва '{name}' веде на невідомий код '{code}'");
        }
    }
}
