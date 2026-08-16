using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class TrackerUrlRulesTests
{
    // Трекери в кожної дисципліни свої — список хостів навмисно відкритий.
    [Theory]
    [InlineData("https://tracker.gg/valorant/match/abc-123")]
    [InlineData("https://www.hltv.org/matches/2370000/team-a-vs-team-b")]
    [InlineData("https://www.dotabuff.com/matches/1234567890")]
    [InlineData("https://op.gg/summoners/euw/Player")]
    [InlineData("https://www.faceit.com/en/csgo/room/1-2-3")]
    public void IsValid_HttpsTracker_IsTrue(string url)
    {
        Assert.True(TrackerUrlRules.IsValid(url));
    }

    [Theory]
    [InlineData("http://tracker.gg/valorant/match/abc")]      // не https
    [InlineData("javascript:alert(1)")]                        // не URL сторінки
    [InlineData("ftp://tracker.gg/x")]
    [InlineData("/relative/path")]
    [InlineData("tracker.gg/valorant")]                        // без схеми
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsValid_Rejected(string? url)
    {
        Assert.False(TrackerUrlRules.IsValid(url));
    }

    // Колонка обмежена 300 символами — довше посилання обірвалося б у базі.
    [Fact]
    public void IsValid_TooLong_IsFalse()
    {
        var url = "https://tracker.gg/" + new string('a', TrackerUrlRules.MaxLength);
        Assert.False(TrackerUrlRules.IsValid(url));
    }

    [Fact]
    public void IsValid_ExactlyMaxLength_IsTrue()
    {
        const string prefix = "https://tracker.gg/";
        var url = prefix + new string('a', TrackerUrlRules.MaxLength - prefix.Length);

        Assert.Equal(TrackerUrlRules.MaxLength, url.Length);
        Assert.True(TrackerUrlRules.IsValid(url));
    }
}
