using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class TrainingRunStatusesTests
{
    [Fact]
    public void All_ContainsEveryStatus()
    {
        Assert.Equal(
            new[] { "Queued", "Running", "Succeeded", "Failed" },
            TrainingRunStatuses.All);
    }

    [Theory]
    [InlineData("Queued")]
    [InlineData("Running")]
    [InlineData("Succeeded")]
    [InlineData("Failed")]
    public void IsValid_KnownStatus_True(string status) => Assert.True(TrainingRunStatuses.IsValid(status));

    [Theory]
    [InlineData("queued")]
    [InlineData("InProgress")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_UnknownStatus_False(string? status) => Assert.False(TrainingRunStatuses.IsValid(status));

    [Fact]
    public void IsFinished_SucceededAndFailed_True()
    {
        Assert.True(TrainingRunStatuses.IsFinished(TrainingRunStatuses.Succeeded));
        Assert.True(TrainingRunStatuses.IsFinished(TrainingRunStatuses.Failed));
    }

    [Fact]
    public void IsFinished_QueuedAndRunning_False()
    {
        Assert.False(TrainingRunStatuses.IsFinished(TrainingRunStatuses.Queued));
        Assert.False(TrainingRunStatuses.IsFinished(TrainingRunStatuses.Running));
    }

    [Fact]
    public void IsUnfinished_IsTheComplementOfIsFinished()
    {
        foreach (var status in TrainingRunStatuses.All)
        {
            Assert.NotEqual(TrainingRunStatuses.IsFinished(status), TrainingRunStatuses.IsUnfinished(status));
        }
    }
}
