using TForge.Common;
using Xunit;

namespace TForge.Tests;

public class DraftEngineTests
{
    private const int TeamA = 1;
    private const int TeamB = 2;

    private static DraftEngine.State Cs2(int current, params DraftEngine.StepView[] steps) =>
        new(Games.CS2, DraftStatuses.InProgress, current, steps);

    private static DraftEngine.StepView Ban(int index, int team, string? chosen = null) =>
        new(index, DraftActions.Ban, team, chosen);

    private static DraftEngine.StepView Side(int index, int team, string? chosen = null) =>
        new(index, DraftActions.Side, team, chosen);

    private static DraftEngine.StepView Decider(int index, string? chosen = null) =>
        new(index, DraftActions.Decider, null, chosen);

    // ---- Каталог кроку ----

    [Fact]
    public void CatalogFor_ShooterBan_IsTheMapPool() =>
        Assert.Equal(GameMaps.CS2, DraftEngine.CatalogFor(Games.CS2, DraftActions.Ban));

    [Fact]
    public void CatalogFor_MobaBan_IsTheHeroPool() =>
        Assert.Equal(GameHeroes.Dota2, DraftEngine.CatalogFor(Games.Dota2, DraftActions.Ban));

    [Fact]
    public void CatalogFor_Side_IsTheSidePair() =>
        Assert.Equal(GameSides.CS2, DraftEngine.CatalogFor(Games.CS2, DraftActions.Side));

    [Fact]
    public void CatalogFor_MobaSide_IsRadiantDire() =>
        Assert.Equal(GameSides.Dota2, DraftEngine.CatalogFor(Games.Dota2, DraftActions.Side));

    [Fact]
    public void CatalogFor_UnknownGame_IsEmpty() =>
        Assert.Empty(DraftEngine.CatalogFor("Overwatch", DraftActions.Ban));

    // ---- Що ще можна взяти ----

    [Fact]
    public void AvailableValues_ExcludesWhatIsAlreadyBanned()
    {
        var state = Cs2(1, Ban(0, TeamA, "Mirage"), Ban(1, TeamB));

        Assert.DoesNotContain("Mirage", DraftEngine.AvailableValues(state, 1));
        Assert.Equal(GameMaps.CS2.Length - 1, DraftEngine.AvailableValues(state, 1).Count);
    }

    [Fact]
    public void AvailableValues_SideIsNeverConsumed()
    {
        // «CT» обирають на кожній карті наново — сторона не витрачається.
        var state = Cs2(2, Ban(0, TeamA, "Mirage"), Side(1, TeamB, "CT"), Side(2, TeamA));

        Assert.Equal(GameSides.CS2, DraftEngine.AvailableValues(state, 2));
    }

    [Fact]
    public void AvailableValues_BannedMapDoesNotShrinkTheSideChoice()
    {
        // Зворотний бік того самого: бан карти не має чіпати вибір сторони.
        var state = Cs2(1, Ban(0, TeamA, "Mirage"), Side(1, TeamB));

        Assert.Equal(2, DraftEngine.AvailableValues(state, 1).Count);
    }

    [Fact]
    public void AvailableValues_DeciderIsTheSingleRemainingMap()
    {
        var steps = new[]
        {
            Ban(0, TeamA, "Ancient"), Ban(1, TeamB, "Anubis"), Ban(2, TeamA, "Dust2"),
            Ban(3, TeamB, "Inferno"), Ban(4, TeamA, "Mirage"), Ban(5, TeamB, "Nuke"),
            Decider(6)
        };

        var available = DraftEngine.AvailableValues(Cs2(6, steps), 6);

        Assert.Single(available);
        Assert.Equal("Overpass", available[0]);
    }

    [Fact]
    public void AvailableValues_UnknownStep_IsEmpty() =>
        Assert.Empty(DraftEngine.AvailableValues(Cs2(0, Ban(0, TeamA)), 42));

    // ---- Коли хід дозволено ----

    [Fact]
    public void CanApply_CurrentStepAndFreeValue_IsAllowed()
    {
        var verdict = DraftEngine.CanApply(Cs2(0, Ban(0, TeamA)), 0, "Mirage");

        Assert.True(verdict.IsAllowed);
        Assert.Null(verdict.Reason);
    }

    [Fact]
    public void CanApply_StaleStepIndex_IsRefused()
    {
        // Капітан натиснув рівно тоді, коли таймер уже зрушив хід.
        var state = Cs2(1, Ban(0, TeamA, "Mirage"), Ban(1, TeamB));

        var verdict = DraftEngine.CanApply(state, 0, "Nuke");

        Assert.False(verdict.IsAllowed);
        Assert.NotNull(verdict.Reason);
    }

    [Fact]
    public void CanApply_AlreadyTakenValue_IsRefused() =>
        Assert.False(DraftEngine.CanApply(
            Cs2(1, Ban(0, TeamA, "Mirage"), Ban(1, TeamB)), 1, "Mirage").IsAllowed);

    [Fact]
    public void CanApply_ValueOutsideCatalog_IsRefused() =>
        Assert.False(DraftEngine.CanApply(Cs2(0, Ban(0, TeamA)), 0, "Cache").IsAllowed);

    [Fact]
    public void CanApply_SideValueOnASideStep_IsAllowed() =>
        Assert.True(DraftEngine.CanApply(Cs2(0, Side(0, TeamA)), 0, "CT").IsAllowed);

    [Fact]
    public void CanApply_MapValueOnASideStep_IsRefused() =>
        Assert.False(DraftEngine.CanApply(Cs2(0, Side(0, TeamA)), 0, "Mirage").IsAllowed);

    [Fact]
    public void CanApply_SameSideTwiceOnDifferentMaps_IsAllowed()
    {
        var state = Cs2(1, Side(0, TeamA, "CT"), Side(1, TeamB));

        Assert.True(DraftEngine.CanApply(state, 1, "CT").IsAllowed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CanApply_BlankValue_IsRefused(string? value) =>
        Assert.False(DraftEngine.CanApply(Cs2(0, Ban(0, TeamA)), 0, value).IsAllowed);

    [Fact]
    public void CanApply_UnknownStepIndex_IsRefused() =>
        Assert.False(DraftEngine.CanApply(Cs2(5, Ban(0, TeamA)), 5, "Mirage").IsAllowed);

    [Fact]
    public void CanApply_StepAlreadyChosen_IsRefused() =>
        Assert.False(DraftEngine.CanApply(Cs2(0, Ban(0, TeamA, "Nuke")), 0, "Mirage").IsAllowed);

    [Theory]
    [InlineData("Pending")]
    [InlineData("Completed")]
    [InlineData("Cancelled")]
    public void CanApply_DraftNotInProgress_IsRefused(string status)
    {
        var state = new DraftEngine.State(Games.CS2, status, 0, new[] { Ban(0, TeamA) });

        Assert.False(DraftEngine.CanApply(state, 0, "Mirage").IsAllowed);
    }

    [Fact]
    public void CanApply_EveryRefusalCarriesAReason()
    {
        // Причина йде користувачеві як текст помилки — порожньої бути не може.
        var verdict = DraftEngine.CanApply(Cs2(0, Ban(0, TeamA)), 0, "Cache");

        Assert.False(verdict.IsAllowed);
        Assert.False(string.IsNullOrWhiteSpace(verdict.Reason));
    }

    // ---- Кінець драфту ----

    [Fact]
    public void IsLastStep_FinalIndex_IsTrue() =>
        Assert.True(DraftEngine.IsLastStep(Cs2(1, Ban(0, TeamA, "Mirage"), Ban(1, TeamB)), 1));

    [Fact]
    public void IsLastStep_NotFinal_IsFalse() =>
        Assert.False(DraftEngine.IsLastStep(Cs2(0, Ban(0, TeamA), Ban(1, TeamB)), 0));

    [Fact]
    public void IsLastStep_NoSteps_IsFalse() =>
        Assert.False(DraftEngine.IsLastStep(Cs2(0), 0));

    // ---- Ціла послідовність проходить від початку до кінця ----

    [Fact]
    public void WholeCs2Bo1Sequence_CanBePlayedThrough()
    {
        // Найдешевша перевірка того, що каталог, послідовність і рушій
        // узгоджені між собою: кожен крок мусить мати що обрати.
        var sequence = DraftSequences.For(Games.CS2, "BO1");
        var steps = sequence
            .Select((template, index) => new DraftEngine.StepView(
                index,
                template.Action,
                template.IsFirstTurnTeam switch { true => TeamA, false => TeamB, null => null },
                null))
            .ToList();

        for (var index = 0; index < steps.Count; index++)
        {
            var state = new DraftEngine.State(Games.CS2, DraftStatuses.InProgress, index, steps);
            var available = DraftEngine.AvailableValues(state, index);

            Assert.NotEmpty(available);
            Assert.True(DraftEngine.CanApply(state, index, available[0]).IsAllowed);

            steps[index] = steps[index] with { ChosenValue = available[0] };
        }
    }

    [Fact]
    public void WholeDota2Sequence_NeverRunsOutOfHeroes()
    {
        var sequence = DraftSequences.For(Games.Dota2, "BO1");
        var steps = sequence
            .Select((template, index) => new DraftEngine.StepView(
                index,
                template.Action,
                template.IsFirstTurnTeam switch { true => TeamA, false => TeamB, null => null },
                null))
            .ToList();

        for (var index = 0; index < steps.Count; index++)
        {
            var state = new DraftEngine.State(Games.Dota2, DraftStatuses.InProgress, index, steps);
            var available = DraftEngine.AvailableValues(state, index);

            Assert.NotEmpty(available);
            steps[index] = steps[index] with { ChosenValue = available[0] };
        }
    }
}
