using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class ChessTrainingPolicyTests
{
    // ---- Черга: один запуск одночасно ----

    [Fact]
    public void CanQueue_NothingUnfinished_May() => Assert.True(ChessTrainingPolicy.CanQueue(false));

    [Fact]
    public void CanQueue_SomethingUnfinished_MayNot() => Assert.False(ChessTrainingPolicy.CanQueue(true));

    // ---- Активувати можна лише успішний запуск ----

    [Fact]
    public void CanActivate_Succeeded_May() =>
        Assert.True(ChessTrainingPolicy.CanActivate(TrainingRunStatuses.Succeeded));

    [Theory]
    [InlineData("Queued")]
    [InlineData("Running")]
    [InlineData("Failed")]
    public void CanActivate_NotSucceeded_MayNot(string status) =>
        Assert.False(ChessTrainingPolicy.CanActivate(status));

    // ---- Активну версію не видаляють ----

    [Fact]
    public void CanDeleteRun_SucceededAndInactive_May() =>
        Assert.True(ChessTrainingPolicy.CanDeleteRun(TrainingRunStatuses.Succeeded, isActive: false));

    [Fact]
    public void CanDeleteRun_Active_MayNot() =>
        Assert.False(ChessTrainingPolicy.CanDeleteRun(TrainingRunStatuses.Succeeded, isActive: true));

    [Fact]
    public void CanDeleteRun_Failed_May() =>
        Assert.True(ChessTrainingPolicy.CanDeleteRun(TrainingRunStatuses.Failed, isActive: false));

    [Theory]
    [InlineData("Queued")]
    [InlineData("Running")]
    public void CanDeleteRun_Unfinished_MayNot(string status) =>
        Assert.False(ChessTrainingPolicy.CanDeleteRun(status, isActive: false));

    // ---- Датасет тримають його запуски ----

    [Fact]
    public void CanDeleteDataset_NoRuns_May() => Assert.True(ChessTrainingPolicy.CanDeleteDataset(false));

    [Fact]
    public void CanDeleteDataset_HasRuns_MayNot() => Assert.False(ChessTrainingPolicy.CanDeleteDataset(true));
}
