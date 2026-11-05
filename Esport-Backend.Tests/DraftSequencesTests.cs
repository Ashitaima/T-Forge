using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class DraftSequencesTests
{
    private static int Count(string game, string format, string action) =>
        DraftSequences.For(game, format).Count(s => s.Action == action);

    private static int CountFor(string game, string format, string action, bool firstTurnTeam) =>
        DraftSequences.For(game, format).Count(s => s.Action == action && s.IsFirstTurnTeam == firstTurnTeam);

    /// <summary>Скільки кроків у зрізі індексів належать команді A.</summary>
    private static int CountInRange(string game, int fromIndex, int toIndex, bool firstTurnTeam)
    {
        var steps = DraftSequences.For(game, "BO1");
        return steps
            .Skip(fromIndex)
            .Take(toIndex - fromIndex + 1)
            .Count(s => s.IsFirstTurnTeam == firstTurnTeam);
    }

    // ---- Вето карт ----

    [Theory]
    [InlineData("BO1", 8)]
    [InlineData("BO3", 10)]
    [InlineData("BO5", 12)]
    public void Cs2_StepCountMatchesFormat(string format, int expected) =>
        Assert.Equal(expected, DraftSequences.For(Games.CS2, format).Count);

    [Fact]
    public void Valorant_MatchesCs2Shape() =>
        Assert.Equal(
            DraftSequences.For(Games.CS2, "BO3").Select(s => (s.Action, s.IsFirstTurnTeam)),
            DraftSequences.For(Games.Valorant, "BO3").Select(s => (s.Action, s.IsFirstTurnTeam)));

    [Theory]
    [InlineData("BO1")]
    [InlineData("BO3")]
    [InlineData("BO5")]
    public void Cs2_UsesWholeSevenMapPool(string format)
    {
        // Бани + піки + вирішальна мусять з'їсти рівно пул: інакше або
        // лишається зайва карта, або кроків більше, ніж є що обирати.
        var consumed = Count(Games.CS2, format, DraftActions.Ban)
            + Count(Games.CS2, format, DraftActions.Pick)
            + Count(Games.CS2, format, DraftActions.Decider);

        Assert.Equal(GameMaps.CS2.Length, consumed);
    }

    [Theory]
    [InlineData("BO1")]
    [InlineData("BO3")]
    [InlineData("BO5")]
    public void Cs2_HasExactlyOneDecider(string format) =>
        Assert.Equal(1, Count(Games.CS2, format, DraftActions.Decider));

    [Theory]
    [InlineData("BO1")]
    [InlineData("BO3")]
    [InlineData("BO5")]
    public void Cs2_BansAreSplitEvenly(string format) =>
        Assert.Equal(
            CountFor(Games.CS2, format, DraftActions.Ban, true),
            CountFor(Games.CS2, format, DraftActions.Ban, false));

    [Theory]
    [InlineData("BO3")]
    [InlineData("BO5")]
    public void Cs2_PicksAreSplitEvenly(string format) =>
        Assert.Equal(
            CountFor(Games.CS2, format, DraftActions.Pick, true),
            CountFor(Games.CS2, format, DraftActions.Pick, false));

    [Theory]
    [InlineData("BO1", 6)]
    [InlineData("BO3", 4)]
    [InlineData("BO5", 2)]
    public void Cs2_MapsPlayedMatchesFormat(string format, int expectedUnplayed)
    {
        // BO1 грає одну карту, BO3 — три, BO5 — п'ять; решта пулу відсіяна.
        var played = Count(Games.CS2, format, DraftActions.Pick)
            + Count(Games.CS2, format, DraftActions.Decider);

        Assert.Equal(GameMaps.CS2.Length - expectedUnplayed, played);
    }

    [Fact]
    public void Cs2_EveryPlayedMapGetsASide() =>
        // Скільки карт грають, стільки разів і обирають сторону.
        Assert.Equal(
            Count(Games.CS2, "BO5", DraftActions.Pick) + Count(Games.CS2, "BO5", DraftActions.Decider),
            Count(Games.CS2, "BO5", DraftActions.Side));

    [Fact]
    public void Cs2_FirstStepBelongsToFirstTurnTeam() =>
        Assert.True(DraftSequences.For(Games.CS2, "BO3")[0].IsFirstTurnTeam);

    [Fact]
    public void Decider_HasNoActingTeam() =>
        Assert.All(
            DraftSequences.For(Games.CS2, "BO3").Where(s => s.Action == DraftActions.Decider),
            step => Assert.Null(step.IsFirstTurnTeam));

    [Fact]
    public void Decider_IsNeverTheFirstStep() =>
        Assert.NotEqual(DraftActions.Decider, DraftSequences.For(Games.CS2, "BO1")[0].Action);

    [Fact]
    public void OnlyDeciderHasNoActingTeam() =>
        Assert.All(
            DraftSequences.For(Games.CS2, "BO5").Where(s => s.Action != DraftActions.Decider),
            step => Assert.NotNull(step.IsFirstTurnTeam));

    // ---- Dota 2: Captains Mode, порядок патча 7.34 ----

    [Fact]
    public void Dota2_HasTwentyFiveSteps() => Assert.Equal(25, DraftSequences.For(Games.Dota2, "BO1").Count);

    [Fact]
    public void Dota2_HasFourteenBansSplitSeven()
    {
        Assert.Equal(14, Count(Games.Dota2, "BO1", DraftActions.Ban));
        Assert.Equal(7, CountFor(Games.Dota2, "BO1", DraftActions.Ban, true));
        Assert.Equal(7, CountFor(Games.Dota2, "BO1", DraftActions.Ban, false));
    }

    [Fact]
    public void Dota2_HasTenPicksSplitFive()
    {
        Assert.Equal(10, Count(Games.Dota2, "BO1", DraftActions.Pick));
        Assert.Equal(5, CountFor(Games.Dota2, "BO1", DraftActions.Pick, true));
        Assert.Equal(5, CountFor(Games.Dota2, "BO1", DraftActions.Pick, false));
    }

    [Fact]
    public void Dota2_BanPhasesAreThreeTwoTwoAndFourOneTwo()
    {
        // 7.34: команда з першим піком банить 3-2-2, команда з другим — 4-1-2.
        // Індекси зсунуто на один: нульовий крок — вибір сторони.
        Assert.Equal(3, CountInRange(Games.Dota2, 1, 7, true));
        Assert.Equal(4, CountInRange(Games.Dota2, 1, 7, false));

        Assert.Equal(2, CountInRange(Games.Dota2, 10, 12, true));
        Assert.Equal(1, CountInRange(Games.Dota2, 10, 12, false));

        Assert.Equal(2, CountInRange(Games.Dota2, 19, 22, true));
        Assert.Equal(2, CountInRange(Games.Dota2, 19, 22, false));
    }

    [Fact]
    public void Dota2_PickPhasesAreOneThreeOne()
    {
        Assert.Equal(1, CountInRange(Games.Dota2, 8, 9, true));
        Assert.Equal(1, CountInRange(Games.Dota2, 8, 9, false));

        Assert.Equal(3, CountInRange(Games.Dota2, 13, 18, true));
        Assert.Equal(3, CountInRange(Games.Dota2, 13, 18, false));

        Assert.Equal(1, CountInRange(Games.Dota2, 23, 24, true));
        Assert.Equal(1, CountInRange(Games.Dota2, 23, 24, false));
    }

    [Fact]
    public void Dota2_StartsWithSideChosenBySecondTeam()
    {
        var first = DraftSequences.For(Games.Dota2, "BO1")[0];
        Assert.Equal(DraftActions.Side, first.Action);
        Assert.False(first.IsFirstTurnTeam);
    }

    [Fact]
    public void Dota2_HasNoDecider() => Assert.Equal(0, Count(Games.Dota2, "BO1", DraftActions.Decider));

    [Fact]
    public void Moba_FormatDoesNotChangeTheSequence() =>
        // Драфт один на матч навіть у серії — див. спеку, «Що свідомо не робимо».
        Assert.Equal(
            DraftSequences.For(Games.Dota2, "BO1").Count,
            DraftSequences.For(Games.Dota2, "BO5").Count);

    // ---- LoL ----

    [Fact]
    public void Lol_HasTwentyOneSteps() =>
        Assert.Equal(21, DraftSequences.For(Games.LeagueOfLegends, "BO1").Count);

    [Fact]
    public void Lol_HasTenBansAndTenPicksSplitEvenly()
    {
        Assert.Equal(5, CountFor(Games.LeagueOfLegends, "BO1", DraftActions.Ban, true));
        Assert.Equal(5, CountFor(Games.LeagueOfLegends, "BO1", DraftActions.Ban, false));
        Assert.Equal(5, CountFor(Games.LeagueOfLegends, "BO1", DraftActions.Pick, true));
        Assert.Equal(5, CountFor(Games.LeagueOfLegends, "BO1", DraftActions.Pick, false));
    }

    [Fact]
    public void Lol_FirstPickPhaseIsB1R1R2B2B3R3()
    {
        var steps = DraftSequences.For(Games.LeagueOfLegends, "BO1");
        var picks = steps.Skip(7).Take(6).Select(s => s.IsFirstTurnTeam).ToArray();

        Assert.Equal(new bool?[] { true, false, false, true, true, false }, picks);
    }

    // ---- Спільне для всіх дисциплін ----

    [Fact]
    public void Moba_HasExactlyOneSideStep()
    {
        Assert.Equal(1, Count(Games.Dota2, "BO1", DraftActions.Side));
        Assert.Equal(1, Count(Games.LeagueOfLegends, "BO1", DraftActions.Side));
    }

    [Theory]
    [InlineData("Overwatch", "BO1")]
    [InlineData(null, "BO1")]
    [InlineData(Games.CS2, "BO7")]
    [InlineData(Games.CS2, null)]
    public void For_Unknown_IsEmpty(string? game, string? format) =>
        Assert.Empty(DraftSequences.For(game, format));

    private static readonly string[] DraftGames =
        { Games.CS2, Games.Valorant, Games.Dota2, Games.LeagueOfLegends };

    [Fact]
    public void HasDraft_EveryDraftGame_IsTrue() =>
        Assert.All(DraftGames, game => Assert.True(DraftSequences.HasDraft(game)));

    [Fact]
    public void HasDraft_Chess_IsFalse() =>
        // Шахи — дисципліна без вето: ні карт, ні героїв. Колись HasDraft
        // означав «гра валідна», і шаховий матч пропонував би драфт без послідовності.
        Assert.False(DraftSequences.HasDraft(Games.Chess));

    [Fact]
    public void For_Chess_IsEmpty() => Assert.Empty(DraftSequences.For(Games.Chess, "BO1"));

    [Fact]
    public void HasDraft_Unknown_IsFalse() => Assert.False(DraftSequences.HasDraft("Overwatch"));

    [Fact]
    public void Every_ActionIsAKnownAction() =>
        Assert.All(
            DraftGames,
            game => Assert.All(
                DraftSequences.For(game, "BO1"),
                step => Assert.True(DraftActions.IsValid(step.Action))));

    [Fact]
    public void Every_MobaStepFitsItsHeroPool()
    {
        // Банів і піків разом не може бути більше, ніж героїв у каталозі.
        foreach (var game in new[] { Games.Dota2, Games.LeagueOfLegends })
        {
            var consumed = Count(game, "BO1", DraftActions.Ban) + Count(game, "BO1", DraftActions.Pick);
            Assert.True(consumed <= GameHeroes.ForGame(game).Length);
        }
    }
}
