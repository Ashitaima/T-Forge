using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class StreamUrlRulesTests
{
    [Theory]
    [InlineData("https://twitch.tv/s1mple")]
    [InlineData("https://www.twitch.tv/s1mple")]
    [InlineData("https://youtube.com/watch?v=abc123")]
    [InlineData("https://www.youtube.com/watch?v=abc123")]
    [InlineData("https://youtu.be/abc123")]
    [InlineData("HTTPS://WWW.TWITCH.TV/s1mple")]
    public void IsValid_AllowedHost_IsTrue(string url)
    {
        Assert.True(StreamUrlRules.IsValid(url));
    }

    // Найважливіший випадок: підрядок "twitch.tv" тут є, але домен чужий.
    // Саме тому хост порівнюється повним збігом.
    [Fact]
    public void IsValid_LookalikeDomain_IsFalse()
    {
        Assert.False(StreamUrlRules.IsValid("https://twitch.tv.evil.com/stream"));
    }

    [Theory]
    [InlineData("http://twitch.tv/s1mple")]
    [InlineData("https://evil.com/stream")]
    [InlineData("https://nottwitch.tv/stream")]
    [InlineData("https://myyoutube.com/watch")]
    [InlineData("notaurl")]
    [InlineData("/relative/path")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsValid_Rejected(string? url)
    {
        Assert.False(StreamUrlRules.IsValid(url));
    }

    [Fact]
    public void AllowedHosts_HaveNoDuplicates()
    {
        Assert.Equal(StreamUrlRules.AllowedHosts.Length, StreamUrlRules.AllowedHosts.Distinct().Count());
    }
}
