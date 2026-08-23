using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class TeamOpeningStatusTests
{
    [Fact]
    public void All_ListsEveryStatus() =>
        Assert.Equal(new[] { "Open", "Filled", "Cancelled" }, TeamOpeningStatus.All);

    [Fact]
    public void IsValid_KnownStatus_Yes() =>
        Assert.True(TeamOpeningStatus.IsValid(TeamOpeningStatus.Filled));

    [Fact]
    public void IsValid_UnknownStatus_No() => Assert.False(TeamOpeningStatus.IsValid("Closed"));

    [Fact]
    public void IsValid_Null_No() => Assert.False(TeamOpeningStatus.IsValid(null));
}
