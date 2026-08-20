using TForge.Common;
using Xunit;

namespace TForge.Tests;

/// <summary>
/// Калькулятор чистий, тож на нього припадає вся вага перевірки правил
/// рейтингу: сервіс лише читає й пише рядки.
/// </summary>
public class EloCalculatorTests
{
    private const int Settled = EloCalculator.ProvisionalMatches; // ознайомчий період позаду

    // ---- Що взагалі рейтингується ----

    [Fact]
    public void IsRated_TournamentMatchWithWinner_Yes()
    {
        Assert.True(EloCalculator.IsRated(7, MatchStatus.Completed, winnerTeamId: 3, awayTeamId: 2));
    }

    [Fact]
    public void IsRated_Friendly_No()
    {
        // Товариський матч — це TournamentId == null. Два капітани можуть
        // викликати одне одного скільки завгодно, драбини це не стосується.
        Assert.False(EloCalculator.IsRated(null, MatchStatus.Completed, winnerTeamId: 3, awayTeamId: 2));
    }

    [Fact]
    public void IsRated_Draw_No()
    {
        // Нічия не рейтингується — так само, як її не рахують
        // PlayerRecordCalculator і TeamRecordCalculator.
        Assert.False(EloCalculator.IsRated(7, MatchStatus.Completed, winnerTeamId: null, awayTeamId: 2));
    }

    [Theory]
    [InlineData(MatchStatus.Scheduled)]
    [InlineData(MatchStatus.InProgress)]
    [InlineData(MatchStatus.Cancelled)]
    [InlineData(MatchStatus.Postponed)]
    public void IsRated_UnfinishedMatch_No(string status)
    {
        Assert.False(EloCalculator.IsRated(7, status, winnerTeamId: 3, awayTeamId: 2));
    }

    [Fact]
    public void IsRated_OpenMatchWithNoOpponent_No()
    {
        // Відкритий матч, до якого ніхто не приєднався: рахувати очікування
        // нема проти кого. Правило тут явне, а не тримається на тому, що
        // відкритими бувають лише практичні матчі.
        Assert.False(EloCalculator.IsRated(7, MatchStatus.Completed, winnerTeamId: 3, awayTeamId: null));
    }

    // ---- Очікуваний результат ----

    [Fact]
    public void ExpectedScore_EqualRatings_IsAHalf()
    {
        Assert.Equal(0.5, EloCalculator.ExpectedScore(1200, 1200), 6);
    }

    [Fact]
    public void ExpectedScore_FourHundredAhead_IsTenToOne()
    {
        Assert.Equal(10.0 / 11.0, EloCalculator.ExpectedScore(1400, 1000), 6);
    }

    [Fact]
    public void ExpectedScore_BothSidesSumToOne()
    {
        var strong = EloCalculator.ExpectedScore(1500, 1100);
        var weak = EloCalculator.ExpectedScore(1100, 1500);

        Assert.Equal(1.0, strong + weak, 6);
    }

    // ---- Приріст ----

    [Fact]
    public void Delta_EqualRatings_IsHalfTheKFactor()
    {
        var k = EloCalculator.KFactor(MatchTypes.GroupStage, Settled);

        Assert.Equal(k / 2, EloCalculator.Delta(1200, 1200, won: true, k));
        Assert.Equal(-k / 2, EloCalculator.Delta(1200, 1200, won: false, k));
    }

    [Fact]
    public void Delta_IsSymmetric_WhenBothSidesUseTheSameK()
    {
        var k = EloCalculator.KFactor(MatchTypes.GroupStage, Settled);

        var winnerGain = EloCalculator.Delta(1450, 1180, won: true, k);
        var loserLoss = EloCalculator.Delta(1180, 1450, won: false, k);

        Assert.Equal(winnerGain, -loserLoss);
    }

    [Fact]
    public void Delta_Underdog_GainsMoreThanFavourite()
    {
        var k = EloCalculator.KFactor(MatchTypes.GroupStage, Settled);

        var underdogWin = EloCalculator.Delta(1000, 1400, won: true, k);
        var favouriteWin = EloCalculator.Delta(1400, 1000, won: true, k);

        Assert.True(underdogWin > favouriteWin);
        Assert.True(underdogWin > 0 && favouriteWin > 0);
    }

    [Fact]
    public void Delta_Favourite_LosesMoreThanUnderdog()
    {
        var k = EloCalculator.KFactor(MatchTypes.GroupStage, Settled);

        var favouriteLoss = EloCalculator.Delta(1400, 1000, won: false, k);
        var underdogLoss = EloCalculator.Delta(1000, 1400, won: false, k);

        Assert.True(favouriteLoss < underdogLoss);
        Assert.True(favouriteLoss < 0 && underdogLoss < 0);
    }

    // ---- K-фактор ----

    [Fact]
    public void KFactor_GroupStage_IsTheTournamentValue()
    {
        Assert.Equal(EloCalculator.TournamentK, EloCalculator.KFactor(MatchTypes.GroupStage, Settled));
    }

    [Theory]
    [InlineData(MatchTypes.Final)]
    [InlineData(MatchTypes.ThirdPlace)]
    public void KFactor_MatchesThatDecidePlaces_MoveTheNumberHarder(string matchType)
    {
        Assert.Equal(EloCalculator.DecisiveK, EloCalculator.KFactor(matchType, Settled));
        Assert.True(EloCalculator.DecisiveK > EloCalculator.TournamentK);
    }

    [Theory]
    [InlineData(MatchTypes.SemiFinal)]
    [InlineData(MatchTypes.QuarterFinal)]
    [InlineData(MatchTypes.RoundOf16)]
    [InlineData(MatchTypes.PlayIn)]
    public void KFactor_OtherRounds_StayAtTheTournamentValue(string matchType)
    {
        Assert.Equal(EloCalculator.TournamentK, EloCalculator.KFactor(matchType, Settled));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    public void KFactor_DuringProvisionalPeriod_IsDoubled(int matchesRated)
    {
        Assert.Equal(
            EloCalculator.TournamentK * 2,
            EloCalculator.KFactor(MatchTypes.GroupStage, matchesRated));
    }

    [Fact]
    public void KFactor_FifthMatch_EndsTheProvisionalPeriod()
    {
        // Ознайомчий період — це перші п'ять матчів, тобто MatchesRated 0..4.
        Assert.Equal(
            EloCalculator.TournamentK * 2,
            EloCalculator.KFactor(MatchTypes.GroupStage, EloCalculator.ProvisionalMatches - 1));
        Assert.Equal(
            EloCalculator.TournamentK,
            EloCalculator.KFactor(MatchTypes.GroupStage, EloCalculator.ProvisionalMatches));
    }

    // ---- Підлога ----

    [Fact]
    public void Apply_NeverFallsBelowTheFloor()
    {
        Assert.Equal(EloCalculator.FloorRating, EloCalculator.Apply(EloCalculator.FloorRating, -50));
        Assert.Equal(EloCalculator.FloorRating, EloCalculator.Apply(120, -900));
    }

    [Fact]
    public void Apply_AboveTheFloor_AddsTheDelta()
    {
        Assert.Equal(1018, EloCalculator.Apply(1000, 18));
        Assert.Equal(982, EloCalculator.Apply(1000, -18));
    }

    // ---- Ліги ----

    [Theory]
    [InlineData(0, RatingTiers.Bronze)]
    [InlineData(1099, RatingTiers.Bronze)]
    [InlineData(1100, RatingTiers.Silver)]
    [InlineData(1249, RatingTiers.Silver)]
    [InlineData(1250, RatingTiers.Gold)]
    [InlineData(1399, RatingTiers.Gold)]
    [InlineData(1400, RatingTiers.Platinum)]
    [InlineData(1549, RatingTiers.Platinum)]
    [InlineData(1550, RatingTiers.Elite)]
    [InlineData(3000, RatingTiers.Elite)]
    public void Tier_ResolvesExactlyAtTheBoundaries(int rating, string expected)
    {
        Assert.Equal(expected, EloCalculator.Tier(rating));
        Assert.Equal(expected, RatingTiers.ForRating(rating));
    }

    [Fact]
    public void Tier_BaseRating_IsBronze()
    {
        // Новачок починає на дні шкали, а не всередині: інакше бронза
        // означала б покарання, а не старт.
        Assert.Equal(RatingTiers.Bronze, EloCalculator.Tier(EloCalculator.BaseRating));
    }

    [Fact]
    public void Tier_BelowTheEloFloor_IsStillBronze()
    {
        // Межі лише зростають, тож ліга однозначна навіть нижче підлоги.
        Assert.Equal(RatingTiers.Bronze, RatingTiers.ForRating(EloCalculator.FloorRating));
        Assert.Equal(RatingTiers.Bronze, RatingTiers.ForRating(-500));
    }

    [Fact]
    public void RatingTiers_KnowsItsOwnKeys()
    {
        Assert.All(RatingTiers.All, tier => Assert.True(RatingTiers.IsValid(tier)));
        Assert.False(RatingTiers.IsValid("Diamond"));
        Assert.False(RatingTiers.IsValid(null));
    }

    // ---- Наскрізний прохід ----

    [Fact]
    public void FullRound_TwoNewbies_EqualAndOpposite()
    {
        // Дві нові команди, обидві ознайомчі, рівні рейтинги: приріст переможця
        // дорівнює втраті переможеного, бо K в обох однаковий.
        var k = EloCalculator.KFactor(MatchTypes.GroupStage, matchesRated: 0);

        var winnerDelta = EloCalculator.Delta(
            EloCalculator.BaseRating, EloCalculator.BaseRating, won: true, k);
        var loserDelta = EloCalculator.Delta(
            EloCalculator.BaseRating, EloCalculator.BaseRating, won: false, k);

        Assert.Equal(EloCalculator.BaseRating + 24, EloCalculator.Apply(EloCalculator.BaseRating, winnerDelta));
        Assert.Equal(EloCalculator.BaseRating - 24, EloCalculator.Apply(EloCalculator.BaseRating, loserDelta));
    }
}
