using TForge.Common;
using Xunit;

namespace TForge.Tests;

/// <summary>
/// Види записів у журналі рейтингу. Значення потрапляють у базу й у відповіді
/// API, тож зафіксовані тестом так само, як MatchChallengeStatus.
/// </summary>
public class RatingChangeKindsTests
{
    [Fact]
    public void All_ContainsBothKinds()
    {
        Assert.Equal(new[] { "Applied", "Reversal" }, RatingChangeKinds.All);
    }

    [Theory]
    [InlineData("Applied")]
    [InlineData("Reversal")]
    public void IsValid_KnownKind_IsTrue(string kind)
    {
        Assert.True(RatingChangeKinds.IsValid(kind));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("applied")]
    [InlineData("Reverted")]
    public void IsValid_UnknownKind_IsFalse(string? kind)
    {
        Assert.False(RatingChangeKinds.IsValid(kind));
    }
}
