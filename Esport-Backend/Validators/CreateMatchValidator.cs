using FluentValidation;
using TForge.Common;
using TForge.DTOs;

namespace TForge.Validators
{
    public class CreateMatchValidator : AbstractValidator<CreateMatchDto>
    {
        public CreateMatchValidator()
        {
            // Порожній турнір — це практичний матч, а не помилка.
            RuleFor(x => x.TournamentId)
                .GreaterThan(0).WithMessage("ID турніру повинен бути більше 0")
                .When(x => x.TournamentId.HasValue);

            // Порожня домашня команда означає «візьми мою» — сервіс підставить
            // її з капітанства; вигадувати id клієнт не мусить.
            RuleFor(x => x.HomeTeamId)
                .GreaterThan(0).WithMessage("ID домашньої команди повинен бути більше 0")
                .When(x => x.HomeTeamId.HasValue);

            // Порожній гість — відкритий матч.
            RuleFor(x => x.AwayTeamId)
                .GreaterThan(0).WithMessage("ID гостьової команди повинен бути більше 0")
                .NotEqual(x => x.HomeTeamId).WithMessage("Команди не можуть грати самі проти себе")
                .When(x => x.AwayTeamId.HasValue);

            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Назва не може перевищувати 100 символів");

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

            // Порожнє поле означає «трансляції немає» — перевіряємо лише заповнене.
            RuleFor(x => x.StreamUrl)
                .Must(StreamUrlRules.IsValid)
                .WithMessage("Посилання має вести на Twitch або YouTube і починатися з https://")
                .When(x => !string.IsNullOrWhiteSpace(x.StreamUrl));

            // Трекер статистики: будь-який https-URL, список хостів не обмежуємо.
            RuleFor(x => x.TrackerUrl)
                .Must(TrackerUrlRules.IsValid)
                .WithMessage("Посилання на трекер має починатися з https:// і бути не довшим за 300 символів")
                .When(x => !string.IsNullOrWhiteSpace(x.TrackerUrl));
        }

        private bool BeValidMatchType(string matchType) => MatchTypes.IsValid(matchType);

        private bool BeValidFormat(string format)
        {
            var validFormats = new[] { "BO1", "BO3", "BO5" };
            return validFormats.Contains(format);
        }
    }
}
