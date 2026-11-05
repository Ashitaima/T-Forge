using TForge.Common;

namespace TForge.Tests;

public class ChessOutcomeTests
{
    private static readonly ChessOutcome O = new(0.5, 0.2, 0.3);

    [Fact]
    public void From_FirstIsWhite_KeepsOrder() =>
        Assert.Equal(new SideOutcome(0.5, 0.2, 0.3), SideOutcome.From(O, firstIsWhite: true));

    [Fact]
    public void From_FirstIsBlack_Swaps() =>
        Assert.Equal(new SideOutcome(0.3, 0.2, 0.5), SideOutcome.From(O, firstIsWhite: false));

    [Fact]
    public void Average_IsComponentwise()
    {
        var a = SideOutcome.Average(new SideOutcome(0.5, 0.2, 0.3), new SideOutcome(0.3, 0.2, 0.5));
        Assert.Equal(0.4, a.FirstWin, 9);
        Assert.Equal(0.2, a.Draw, 9);
        Assert.Equal(0.4, a.SecondWin, 9);
    }

    [Fact]
    public void ExpectedPoints_CountsADrawAsHalf()
    {
        var s = new SideOutcome(0.5, 0.2, 0.3);
        Assert.Equal(0.6, s.FirstExpectedPoints, 9);
        Assert.Equal(0.4, s.SecondExpectedPoints, 9);
    }
}
