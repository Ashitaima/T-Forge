using TForge.Common;

namespace TForge.Tests;

public class ChessFeaturesTests
{
    [Fact]
    public void Vector_MatchesThePythonOrder() =>
        // Той самий випадок, що test_feature_vector_order_and_one_hot у ml/chess.
        Assert.Equal(new[] { 100f, 1550f, 0f, 1f, 0f, 0f },
            ChessFeatures.Vector(1600, 1500, TimeControls.Blitz));

    [Fact]
    public void Vector_HasOneColumnPerTimeControlAfterTheRatings() =>
        Assert.Equal(ChessFeatures.Count, 2 + TimeControls.All.Length);

    [Fact]
    public void Vector_UnknownTimeControl_Throws() =>
        Assert.Throws<ArgumentException>(() => ChessFeatures.Vector(1500, 1500, "Armageddon"));

    [Fact]
    public void Elo_EqualRatings_IsSymmetric()
    {
        var o = EloBaseline.Predict(1500, 1500, 0.1);
        Assert.Equal(o.White, o.Black, 9);
        Assert.Equal(0.1, o.Draw, 9);
    }

    [Fact]
    public void Elo_HugeGap_NeverNegativeAndSumsToOne()
    {
        var o = EloBaseline.Predict(2800, 800, 0.1);
        Assert.True(o.Black >= 0);
        Assert.Equal(1.0, o.White + o.Draw + o.Black, 9);
    }

    [Fact]
    public void Elo_StrongerWhite_IsFavoured() =>
        Assert.True(EloBaseline.Predict(1800, 1400, 0.05).White > 0.8);
}
