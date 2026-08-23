using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class DraftConstantsTests
{
    [Theory]
    [InlineData("Pending")]
    [InlineData("InProgress")]
    [InlineData("Completed")]
    [InlineData("Cancelled")]
    public void Status_Known_IsValid(string status) => Assert.True(DraftStatuses.IsValid(status));

    [Theory]
    [InlineData("pending")]
    [InlineData("Draft")]
    [InlineData("")]
    [InlineData(null)]
    public void Status_Unknown_IsNotValid(string? status) => Assert.False(DraftStatuses.IsValid(status));

    [Fact]
    public void Status_All_HasNoDuplicates() =>
        Assert.Equal(DraftStatuses.All.Length, DraftStatuses.All.Distinct().Count());

    [Theory]
    [InlineData("Ban")]
    [InlineData("Pick")]
    [InlineData("Side")]
    [InlineData("Decider")]
    public void Action_Known_IsValid(string action) => Assert.True(DraftActions.IsValid(action));

    [Theory]
    [InlineData("ban")]
    [InlineData("Veto")]
    [InlineData("")]
    [InlineData(null)]
    public void Action_Unknown_IsNotValid(string? action) => Assert.False(DraftActions.IsValid(action));

    [Fact]
    public void Action_All_HasNoDuplicates() =>
        Assert.Equal(DraftActions.All.Length, DraftActions.All.Distinct().Count());

    // ---- Сторона з пулу не вибуває, решта — вибуває ----

    [Theory]
    [InlineData("Ban")]
    [InlineData("Pick")]
    [InlineData("Decider")]
    public void ConsumesFromPool_ChoiceFromCatalog_IsTrue(string action) =>
        Assert.True(DraftActions.ConsumesFromPool(action));

    [Fact]
    public void ConsumesFromPool_Side_IsFalse() =>
        Assert.False(DraftActions.ConsumesFromPool(DraftActions.Side));
}
