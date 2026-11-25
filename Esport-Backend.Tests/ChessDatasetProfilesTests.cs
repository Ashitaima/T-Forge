using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class ChessDatasetProfilesTests
{
    [Fact]
    public void All_MatchesPythonDatasetsModule()
    {
        // Дзеркало LICHESS/MINIMAL у ml/chess/datasets.py. Значення приходять у
        // JSON-вердикті --validate, тож розбіжність тут — відкинутий датасет.
        Assert.Equal(new[] { "Minimal", "Lichess" }, ChessDatasetProfiles.All);
    }

    [Theory]
    [InlineData("Minimal")]
    [InlineData("Lichess")]
    public void IsValid_KnownProfile_True(string profile) => Assert.True(ChessDatasetProfiles.IsValid(profile));

    [Theory]
    [InlineData("minimal")]
    [InlineData("Kaggle")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_UnknownProfile_False(string? profile) => Assert.False(ChessDatasetProfiles.IsValid(profile));
}
