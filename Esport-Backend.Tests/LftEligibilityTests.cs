using TForge.Common;
using TForge.Models;
using Xunit;

namespace TForge.Tests;

public class LftEligibilityTests
{
    private static LftEligibility.Requirements Wanted(
        string position = "", string minRank = "",
        int? min = null, int? max = null) =>
        new(Games.Valorant, position, minRank, min, max);

    /// <summary>Вільний агент, що шукає команду у Valorant і всім вимогам відповідає.</summary>
    private static Player Candidate(
        string position = "Duelist", string rank = "Ascendant2",
        int? offset = 120, bool looking = true,
        int? teamId = null, string? game = null) =>
        new()
        {
            Id = 1,
            IsActive = true,
            TeamId = teamId,
            UtcOffsetMinutes = offset,
            GameProfiles = new List<PlayerGameProfile>
            {
                new()
                {
                    Game = game ?? Games.Valorant,
                    Position = position,
                    Rank = rank,
                    IsLookingForTeam = looking
                }
            }
        };

    private static bool Matches(LftEligibility.Requirements requirements, Player player) =>
        LftEligibility.Predicate(requirements).Compile()(player);

    // ---- Базовий випадок ----

    [Fact]
    public void FullyMatchingCandidate_IsEligible() =>
        Assert.True(Matches(Wanted("Duelist", "Ascendant1", 0, 180), Candidate()));

    // ---- Вільний агент ----

    [Fact]
    public void PlayerAlreadyOnATeam_IsNotEligible() =>
        Assert.False(Matches(Wanted(), Candidate(teamId: 5)));

    [Fact]
    public void InactivePlayer_IsNotEligible()
    {
        var player = Candidate();
        player.IsActive = false;
        Assert.False(Matches(Wanted(), player));
    }

    // ---- Дисципліна й прапорець ----

    [Fact]
    public void ProfileForAnotherGame_IsNotEligible() =>
        Assert.False(Matches(Wanted(), Candidate(game: Games.CS2)));

    [Fact]
    public void NotLookingForTeam_IsNotEligible() =>
        // Вільний агент, що прапорця не ставив, у стрічку не потрапляє:
        // «без команди» і «шукаю команду» — різні речі.
        Assert.False(Matches(Wanted(), Candidate(looking: false)));

    // ---- Позиція ----

    [Fact]
    public void WrongPosition_IsNotEligible() =>
        Assert.False(Matches(Wanted(position: "Sentinel"), Candidate(position: "Duelist")));

    [Fact]
    public void AnyPosition_AdmitsEveryRole() =>
        Assert.True(Matches(Wanted(position: ""), Candidate(position: "Sentinel")));

    // ---- Ранг ----

    [Fact]
    public void RankExactlyOnTheFloor_IsEligible() =>
        Assert.True(Matches(Wanted(minRank: "Ascendant2"), Candidate(rank: "Ascendant2")));

    [Fact]
    public void RankBelowTheFloor_IsNotEligible() =>
        Assert.False(Matches(Wanted(minRank: "Ascendant2"), Candidate(rank: "Ascendant1")));

    [Fact]
    public void RankAboveTheFloor_IsEligible() =>
        Assert.True(Matches(Wanted(minRank: "Ascendant2"), Candidate(rank: "Radiant")));

    [Fact]
    public void NoRankFloor_AdmitsAPlayerWithoutARank() =>
        Assert.True(Matches(Wanted(minRank: ""), Candidate(rank: "")));

    [Fact]
    public void RankFloor_ExcludesAPlayerWithoutARank() =>
        Assert.False(Matches(Wanted(minRank: "Ascendant2"), Candidate(rank: "")));

    // ---- Часовий пояс ----

    [Fact]
    public void OffsetInsideTheWindow_IsEligible() =>
        Assert.True(Matches(Wanted(min: 0, max: 180), Candidate(offset: 120)));

    [Fact]
    public void OffsetOutsideTheWindow_IsNotEligible() =>
        Assert.False(Matches(Wanted(min: 0, max: 180), Candidate(offset: 600)));

    [Fact]
    public void NoWindow_AdmitsAPlayerWithoutAnOffset() =>
        Assert.True(Matches(Wanted(min: null, max: null), Candidate(offset: null)));

    [Fact]
    public void Window_ExcludesAPlayerWithoutAnOffset() =>
        Assert.False(Matches(Wanted(min: 0, max: 180), Candidate(offset: null)));

    [Fact]
    public void HalfOpenWindow_IsRespected()
    {
        Assert.True(Matches(Wanted(min: 0, max: null), Candidate(offset: 600)));
        Assert.False(Matches(Wanted(min: 0, max: null), Candidate(offset: -60)));
    }

    // ---- Узгодженість із TimeZones.IsWithin ----

    [Theory]
    [InlineData(120, 0, 180)]
    [InlineData(600, 0, 180)]
    [InlineData(null, 0, 180)]
    [InlineData(null, null, null)]
    [InlineData(120, null, null)]
    [InlineData(-60, 0, null)]
    [InlineData(600, null, 180)]
    public void OffsetWindow_AgreesWithTimeZonesIsWithin(int? offset, int? min, int? max)
    {
        // Предикат мусить повторювати TimeZones.IsWithin, а не тлумачити його
        // по-своєму. Вбудований він тому, що EF не вміє перекласти виклик
        // статичного методу в SQL, — але два записи одного правила розходяться
        // тихо, і цей тест єдиний, хто це помітить.
        var viaPredicate = Matches(Wanted(min: min, max: max), Candidate(offset: offset));

        Assert.Equal(TimeZones.IsWithin(offset, min, max), viaPredicate);
    }

    // ---- From ----

    [Fact]
    public void From_CopiesEveryRequirement()
    {
        var opening = new TeamOpening
        {
            Game = Games.CS2, Position = "AWPer", MinRank = "Premier20000",
            MinUtcOffsetMinutes = 60, MaxUtcOffsetMinutes = 180
        };

        var requirements = LftEligibility.From(opening);

        Assert.Equal(new LftEligibility.Requirements(
            Games.CS2, "AWPer", "Premier20000", 60, 180), requirements);
    }
}
