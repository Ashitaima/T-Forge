using FluentValidation;
using Computational_Practice.DTOs;

namespace Computational_Practice.Validators
{
    public class CreateMatchValidator : AbstractValidator<CreateMatchDto>
    {
        public CreateMatchValidator()
        {
            RuleFor(x => x.TournamentId)
                .GreaterThan(0).WithMessage("ID турніру повинен бути більше 0");

            RuleFor(x => x.HomeTeamId)
                .GreaterThan(0).WithMessage("ID домашньої команди повинен бути більше 0");

            RuleFor(x => x.AwayTeamId)
                .GreaterThan(0).WithMessage("ID гостьової команди повинен бути більше 0")
                .NotEqual(x => x.HomeTeamId).WithMessage("Команди не можуть грати самі проти себе");

            RuleFor(x => x.ScheduledAt)
                .NotEmpty().WithMessage("Час проведення матчу є обов'язковим")
                .GreaterThan(DateTime.Now).WithMessage("Час проведення матчу повинен бути в майбутньому");

            RuleFor(x => x.MatchType)
                .NotEmpty().WithMessage("Тип матчу є обов'язковим")
                .Must(BeValidMatchType).WithMessage("Некоректний тип матчу");

            RuleFor(x => x.Format)
                .NotEmpty().WithMessage("Формат матчу є обов'язковим")
                .Must(BeValidFormat).WithMessage("Некоректний формат матчу");

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Примітки не можуть перевищувати 500 символів");
        }

        private bool BeValidMatchType(string matchType)
        {
            var validTypes = new[] { "GroupStage", "QuarterFinal", "SemiFinal", "Final", "ThirdPlace" };
            return validTypes.Contains(matchType);
        }

        private bool BeValidFormat(string format)
        {
            var validFormats = new[] { "BO1", "BO3", "BO5" };
            return validFormats.Contains(format);
        }
    }
}
