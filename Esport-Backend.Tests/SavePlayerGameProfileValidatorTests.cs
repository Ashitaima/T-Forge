using TForge.Common;
using TForge.DTOs;
using TForge.Validators;
using Xunit;

namespace TForge.Tests;

public class SavePlayerGameProfileValidatorTests
{
    private readonly SavePlayerGameProfileValidator _validator = new();

    private static SavePlayerGameProfileDto Dto(string game, string position, string rank) =>
        new() { Game = game, Position = position, Rank = rank };

    [Fact]
    public void RankOfTheSameGame_IsAccepted() =>
        Assert.True(_validator.Validate(Dto(Games.Valorant, "Duelist", "Ascendant2")).IsValid);

    [Fact]
    public void EmptyRank_IsAccepted() =>
        Assert.True(_validator.Validate(Dto(Games.Valorant, "Duelist", "")).IsValid);

    [Fact]
    public void RankOfAnotherGame_IsRejected() =>
        // Radiant існує, але не в CS2 — та сама помилка, що «AWPer у Valorant».
        Assert.False(_validator.Validate(Dto(Games.CS2, "AWPer", "Radiant")).IsValid);

    [Fact]
    public void PositionOfAnotherGame_IsStillRejected() =>
        // Наявне правило не мало постраждати від нового.
        Assert.False(_validator.Validate(Dto(Games.Valorant, "AWPer", "Ascendant2")).IsValid);
}
