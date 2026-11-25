using TForge.Common;
using Xunit;

namespace TForge.Tests;

/// <summary>
/// Розбір вердикту train.py --validate. Чиста функція, тож тестується без
/// Python — сам контракт тримає ml/chess/test_datasets.py з іншого боку.
/// </summary>
public class ChessDatasetSummaryTests
{
    private const string GoodJson = """
        {"profile":"Lichess","rows":15461,
         "by_time_control":{"Bullet":0,"Blitz":4,"Rapid":10607,"Classical":1757},
         "is_time_ordered":true,"issues":[]}
        """;

    private const string RejectedJson = """
        {"profile":null,"rows":0,"by_time_control":{},
         "is_time_ordered":false,"issues":["відсутні колонки: winner"]}
        """;

    [Fact]
    public void Parse_GoodVerdict_IsUsable()
    {
        var summary = ChessDatasetSummary.Parse(GoodJson);

        Assert.True(summary.IsUsable);
        Assert.Equal(ChessDatasetProfiles.Lichess, summary.Profile);
        Assert.Equal(15461, summary.Rows);
        Assert.True(summary.IsTimeOrdered);
        Assert.Equal(0, summary.ByTimeControl["Bullet"]);
        Assert.Equal(10607, summary.ByTimeControl["Rapid"]);
        Assert.Empty(summary.Issues);
    }

    [Fact]
    public void Parse_RejectedVerdict_IsNotUsable()
    {
        var summary = ChessDatasetSummary.Parse(RejectedJson);

        Assert.False(summary.IsUsable);
        Assert.Equal("відсутні колонки: winner", Assert.Single(summary.Issues));
        Assert.Equal(0, summary.Rows);
    }

    [Fact]
    public void Parse_UnknownProfile_IsNotUsable()
    {
        // Профіль, якого немає в каталозі, означає, що Python і C# розійшлися —
        // краще відмовити, ніж зберегти датасет, який нічим не навчити.
        var summary = ChessDatasetSummary.Parse(
            """{"profile":"Kaggle","rows":500,"by_time_control":{},"is_time_ordered":true,"issues":[]}""");

        Assert.False(summary.IsUsable);
    }

    [Fact]
    public void Parse_ZeroRowsWithoutIssues_IsNotUsable()
    {
        var summary = ChessDatasetSummary.Parse(
            """{"profile":"Minimal","rows":0,"by_time_control":{},"is_time_ordered":false,"issues":[]}""");

        Assert.False(summary.IsUsable);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json at all")]
    [InlineData("{\"profile\":")]
    public void Parse_Unparseable_BecomesAnIssueNotAnException(string json)
    {
        // Нерозбірливий stdout — це зламаний інструментарій, і він має
        // доїхати як причина, а не вилетіти винятком із середини сервісу.
        var summary = ChessDatasetSummary.Parse(json);

        Assert.False(summary.IsUsable);
        Assert.NotEmpty(summary.Issues);
    }

    [Fact]
    public void Rejected_CarriesTheIssue()
    {
        var summary = ChessDatasetSummary.Rejected("щось пішло не так");

        Assert.False(summary.IsUsable);
        Assert.Equal("щось пішло не так", Assert.Single(summary.Issues));
    }
}
