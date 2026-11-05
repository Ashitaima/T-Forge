using TForge.Common;
using TForge.DTOs;
using TForge.Validators;

namespace TForge.Tests;

public class ChessFieldValidatorsTests
{
    private static bool Valid(SavePlayerGameProfileDto dto) =>
        new SavePlayerGameProfileValidator().Validate(dto).IsValid;

    [Fact]
    public void ChessProfile_WithRatingInRange_IsValid() =>
        Assert.True(Valid(new SavePlayerGameProfileDto { Game = Games.Chess, Rating = 1850 }));

    [Fact]
    public void ChessProfile_WithoutRating_IsValid() =>
        // Рейтинг необов'язковий: гравець може вказати лише смугу.
        Assert.True(Valid(new SavePlayerGameProfileDto { Game = Games.Chess, Rank = "Chess1400" }));

    [Theory]
    [InlineData(99)]
    [InlineData(3501)]
    public void ChessProfile_RatingOutOfRange_IsInvalid(int rating) =>
        Assert.False(Valid(new SavePlayerGameProfileDto { Game = Games.Chess, Rating = rating }));

    [Fact]
    public void NonChessProfile_WithRating_IsInvalid() =>
        // Точний рейтинг має сенс лише для шахів; у Valorant ранг — це сходинка.
        Assert.False(Valid(new SavePlayerGameProfileDto { Game = Games.Valorant, Rating = 1500 }));

    private static bool ValidDuel(CreateDuelDto dto) =>
        new CreateDuelValidator().Validate(dto).IsValid;

    private static CreateDuelDto Duel(string game, string? timeControl) => new()
    {
        Game = game,
        TimeControl = timeControl,
        ScheduledAt = DateTime.UtcNow.AddDays(1),
        Format = "BO1"
    };

    [Fact]
    public void ChessDuel_WithoutTimeControl_IsInvalid() =>
        Assert.False(ValidDuel(Duel(Games.Chess, null)));

    [Fact]
    public void ChessDuel_WithTimeControl_IsValid() =>
        Assert.True(ValidDuel(Duel(Games.Chess, TimeControls.Blitz)));

    [Fact]
    public void ChessDuel_UnknownTimeControl_IsInvalid() =>
        Assert.False(ValidDuel(Duel(Games.Chess, "Armageddon")));

    [Fact]
    public void Cs2Duel_WithoutTimeControl_IsValid() =>
        Assert.True(ValidDuel(Duel(Games.CS2, null)));

    private static bool ValidTournament(string game, string? timeControl) =>
        new CreateTournamentValidator().Validate(new CreateTournamentDto
        {
            Name = "Chess Cup",
            Description = "Командний рапід",
            Game = game,
            TimeControl = timeControl,
            StartDate = DateTime.UtcNow.AddDays(2),
            EndDate = DateTime.UtcNow.AddDays(5),
            MaxTeams = 8
        }).IsValid;

    [Fact]
    public void ChessTournament_WithoutTimeControl_IsValid() =>
        // Порожній контроль означає рапід — TimeControls.Default.
        Assert.True(ValidTournament(Games.Chess, null));

    [Fact]
    public void Tournament_UnknownTimeControl_IsInvalid() =>
        Assert.False(ValidTournament(Games.Chess, "Armageddon"));
}
