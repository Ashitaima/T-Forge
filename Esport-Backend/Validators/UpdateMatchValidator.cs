using FluentValidation;
using TForge.Common;
using TForge.DTOs;

namespace TForge.Validators
{
    public class UpdateMatchValidator : AbstractValidator<UpdateMatchDto>
    {
        public UpdateMatchValidator()
        {
            RuleFor(x => x.ScheduledAt)
                .NotEmpty().WithMessage("Час проведення матчу є обов'язковим");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Статус матчу є обов'язковим")
                .Must(BeValidStatus).WithMessage("Некоректний статус матчу");

            RuleFor(x => x.HomeTeamScore)
                .GreaterThanOrEqualTo(0).WithMessage("Рахунок домашньої команди не може бути від'ємним");

            RuleFor(x => x.AwayTeamScore)
                .GreaterThanOrEqualTo(0).WithMessage("Рахунок гостьової команди не може бути від'ємним");

            RuleFor(x => x.WinnerTeamId)
                .GreaterThan(0).WithMessage("ID команди-переможця повинен бути більше 0")
                .When(x => x.WinnerTeamId.HasValue);

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Примітки не можуть перевищувати 500 символів");

            RuleFor(x => x.StartedAt)
                .LessThanOrEqualTo(x => x.EndedAt).WithMessage("Час початку повинен бути раніше часу завершення")
                .When(x => x.StartedAt.HasValue && x.EndedAt.HasValue);

            RuleFor(x => x.EndedAt)
                .GreaterThan(x => x.StartedAt).WithMessage("Час завершення повинен бути пізніше часу початку")
                .When(x => x.StartedAt.HasValue && x.EndedAt.HasValue);

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

        private bool BeValidStatus(string status) => MatchStatus.IsValid(status);
    }
}
