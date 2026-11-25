using Microsoft.Extensions.Logging.Abstractions;
using TForge.Common;
using TForge.Services;
using Xunit;

namespace TForge.Tests;

/// <summary>
/// Перемикання версії моделі. Працює на вбудованих файлах — тих самих, що вже
/// потрібні ChessModelParityTests, тож нових залежностей тест не додає.
/// </summary>
public class ChessModelRunnerSwapTests
{
    private static readonly string Baseline =
        Path.Combine(AppContext.BaseDirectory, "MlModels", "chess");

    private static ChessModelRunner Runner() =>
        new(Baseline, NullLogger<ChessModelRunner>.Instance);

    [Fact]
    public void Constructor_LoadsBaseline_IsAvailableWithNoRunId()
    {
        var runner = Runner();

        Assert.True(runner.IsAvailable);
        Assert.Null(runner.ActiveRunId);
        Assert.NotNull(runner.Info);
    }

    [Fact]
    public void Load_SameDirectoryWithRunId_SwapsAndRemembersTheRun()
    {
        var runner = Runner();

        Assert.True(runner.Load(Baseline, runId: 42));
        Assert.True(runner.IsAvailable);
        Assert.Equal(42, runner.ActiveRunId);
    }

    [Fact]
    public void Load_MissingDirectory_KeepsThePreviousSnapshot()
    {
        var runner = Runner();
        var before = runner.Info;

        Assert.False(runner.Load(Path.Combine(Path.GetTempPath(), "tforge-no-such-model"), runId: 7));

        // Найважливіше в усьому класі: зламана версія не має права лишити
        // застосунок зовсім без моделі.
        Assert.True(runner.IsAvailable);
        Assert.Same(before, runner.Info);
        Assert.Null(runner.ActiveRunId);
    }

    [Fact]
    public void Load_DirectoryWithWrongFeatureOrder_IsRefused()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"tforge-bad-order-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            foreach (var name in new[] { "logreg.onnx", "gbt.onnx" })
            {
                File.Copy(Path.Combine(Baseline, name), Path.Combine(directory, name));
            }

            // rating_mean і rating_diff переставлені — модель, навчена з таким
            // порядком, рахувала б не те й нічого не повідомила.
            File.WriteAllText(Path.Combine(directory, "model_info.json"), """
                {"trained_at":"2026-11-23T00:00:00+00:00","dataset":"x.csv",
                 "rows_train":1000,"rows_test":100,
                 "feature_order":["rating_mean","rating_diff","tc_bullet","tc_blitz","tc_rapid","tc_classical"],
                 "class_order":["white","draw","black"],
                 "elo_draw_rate":0.05,"train_games_by_time_control":{},"metrics":{}}
                """);

            var runner = Runner();

            Assert.False(runner.Load(directory, runId: 9));
            Assert.True(runner.IsAvailable);
            Assert.Null(runner.ActiveRunId);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Predict_StillWorksAfterASwap()
    {
        var runner = Runner();
        runner.Load(Baseline, runId: 1);

        var outcomes = runner.Predict(1800, 1400, TimeControls.Rapid);

        Assert.Equal(PredictionModels.All.Length, outcomes.Count);
        foreach (var model in PredictionModels.All)
        {
            var outcome = outcomes[model];
            Assert.InRange(outcome.White + outcome.Draw + outcome.Black, 0.99, 1.01);
        }
    }

    [Fact]
    public void BaselineDirectory_IsWhereTheConstructorLookedFirst()
    {
        Assert.Equal(Baseline, Runner().BaselineDirectory);
    }

    [Fact]
    public void BaselineDataset_IsTheBuiltInSet()
    {
        Assert.Equal("games.csv", Runner().BaselineDataset);
    }

    [Fact]
    public void BaselineDataset_SurvivesSwitchingToAnotherVersion()
    {
        // Спершу це читалося з Info, і після активації користувацької версії
        // підпис «вбудована» показував її датасет (data.csv) замість games.csv.
        var runner = Runner();
        var directory = Path.Combine(Path.GetTempPath(), $"tforge-other-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            foreach (var name in new[] { "logreg.onnx", "gbt.onnx" })
            {
                File.Copy(Path.Combine(Baseline, name), Path.Combine(directory, name));
            }

            var info = File.ReadAllText(Path.Combine(Baseline, "model_info.json"))
                .Replace("\"games.csv\"", "\"data.csv\"");
            File.WriteAllText(Path.Combine(directory, "model_info.json"), info);

            Assert.True(runner.Load(directory, runId: 5));
            Assert.Equal("data.csv", runner.Info!.Dataset);
            Assert.Equal("games.csv", runner.BaselineDataset);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
