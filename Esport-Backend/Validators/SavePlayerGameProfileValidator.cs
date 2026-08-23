using FluentValidation;
using TForge.Common;
using TForge.DTOs;

namespace TForge.Validators
{
    /// <summary>
    /// Позицію перевіряємо разом із дисципліною, а не окремо: «AWPer» —
    /// правильна позиція, але не у Valorant. Саме тому тут GamePositions,
    /// а не плаский PlayerPositions.
    /// </summary>
    public class SavePlayerGameProfileValidator : AbstractValidator<SavePlayerGameProfileDto>
    {
        public SavePlayerGameProfileValidator()
        {
            RuleFor(x => x.Game)
                .NotEmpty().WithMessage("Оберіть дисципліну")
                .Must(Games.IsValid).WithMessage("Невідома дисципліна");

            RuleFor(x => x.Position)
                .Must((dto, position) => GamePositions.IsValidFor(dto.Game, position))
                .WithMessage("Ця позиція не належить обраній дисципліні");

            // Ранг перевіряємо разом із дисципліною з тієї ж причини, що й
            // позицію: «Radiant» — правильний ранг, але не в CS2.
            RuleFor(x => x.Rank)
                .Must((dto, rank) => GameRanks.IsValidFor(dto.Game, rank))
                .WithMessage("Цей ранг не належить обраній дисципліні");
        }
    }
}
