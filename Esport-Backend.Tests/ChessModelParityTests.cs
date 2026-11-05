using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using TForge.Common;
using TForge.Services;

namespace TForge.Tests;

/// <summary>
/// Python і C# рахують ознаки кожен сам. Якщо вони розійдуться, нічого не
/// впаде — моделі просто впевнено скажуть неправду. Цей тест подає ті самі
/// входи, що й ml/chess/train.py, і вимагає ті самі ймовірності.
/// Після перенавчання (python train.py) фікстура й моделі оновлюються разом.
/// </summary>
public class ChessModelParityTests
{
    private sealed record Fixture(int White, int Black, string TimeControl, double[] Logreg, double[] Gbt, double[] Elo);

    private static readonly string Dir = Path.Combine(AppContext.BaseDirectory, "MlModels", "chess");

    private static IReadOnlyList<Fixture> Load() =>
        JsonSerializer.Deserialize<List<Fixture>>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "chess_parity.json")),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower })!;

    [Fact]
    public void Runner_LoadsBothModels()
    {
        using var runner = new ChessModelRunner(Dir, NullLogger<ChessModelRunner>.Instance);
        Assert.True(runner.IsAvailable);
        Assert.NotNull(runner.Info);
    }

    [Fact]
    public void Fixtures_CoverEveryTimeControl() =>
        Assert.Equal(TimeControls.All.OrderBy(x => x), Load().Select(f => f.TimeControl).Distinct().OrderBy(x => x));

    [Fact]
    public void EveryFixture_MatchesPython()
    {
        using var runner = new ChessModelRunner(Dir, NullLogger<ChessModelRunner>.Instance);

        foreach (var f in Load())
        {
            var p = runner.Predict(f.White, f.Black, f.TimeControl);
            AssertClose(f.Logreg, p[PredictionModels.LogisticRegression], f);
            AssertClose(f.Gbt, p[PredictionModels.GradientBoosting], f);
            AssertClose(f.Elo, p[PredictionModels.Elo], f);
        }
    }

    [Fact]
    public void MissingDirectory_DisablesPredictionInsteadOfThrowing()
    {
        using var runner = new ChessModelRunner(Path.Combine(Dir, "nope"), NullLogger<ChessModelRunner>.Instance);
        Assert.False(runner.IsAvailable);
        Assert.Throws<InvalidOperationException>(() => runner.Predict(1500, 1500, TimeControls.Rapid));
    }

    private static void AssertClose(double[] expected, ChessOutcome actual, Fixture f)
    {
        var a = new[] { actual.White, actual.Draw, actual.Black };
        for (var i = 0; i < 3; i++)
        {
            Assert.True(Math.Abs(expected[i] - a[i]) < 1e-5,
                $"{f.White} vs {f.Black} {f.TimeControl}: class {i} expected {expected[i]}, got {a[i]}");
        }
    }
}
