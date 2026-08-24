using FluentValidation;
using TForge.Common;
using TForge.DTOs;

namespace TForge.Validators
{
    /// <summary>
    /// Вимоги вакансії перевіряємо разом із дисципліною, як і в профілі
    /// гравця: «AWPer» та «Premier20000» правильні лише в CS2. Порожнє
    /// значення скрізь означає «будь-який» і помилкою не є.
    /// </summary>
    public class CreateTeamOpeningValidator : AbstractValidator<CreateTeamOpeningDto>
    {
        public CreateTeamOpeningValidator()
        {
            RuleFor(x => x.TeamId)
                .GreaterThan(0).WithMessage("Оберіть команду");

            RuleFor(x => x.Game)
                .NotEmpty().WithMessage("Оберіть дисципліну")
                .Must(Games.IsValid).WithMessage("Невідома дисципліна");

            RuleFor(x => x.Position)
                .Must((dto, position) => GamePositions.IsValidFor(dto.Game, position))
                .WithMessage("Ця позиція не належить обраній дисципліні");

            RuleFor(x => x.MinRank)
                .Must((dto, rank) => GameRanks.IsValidFor(dto.Game, rank))
                .WithMessage("Цей ранг не належить обраній дисципліні");

            RuleFor(x => x.MinUtcOffsetMinutes)
                .Must(TimeZones.IsValid).WithMessage("Невідомий часовий пояс");

            RuleFor(x => x.MaxUtcOffsetMinutes)
                .Must(TimeZones.IsValid).WithMessage("Невідомий часовий пояс");

            // Перевернуте вікно не відкидає нікого — воно просто не має сенсу,
            // і капітан помітить це лише з порожньої стрічки.
            RuleFor(x => x)
                .Must(dto => dto.MinUtcOffsetMinutes == null
                          || dto.MaxUtcOffsetMinutes == null
                          || dto.MinUtcOffsetMinutes <= dto.MaxUtcOffsetMinutes)
                .WithMessage("Початок вікна часових поясів має бути не пізніше за кінець");

            RuleFor(x => x.Pitch)
                .MaximumLength(300).WithMessage("Опис задовгий: максимум 300 символів");
        }
    }
}
