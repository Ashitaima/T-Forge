using FluentValidation;
using TForge.Common;
using TForge.DTOs;

namespace TForge.Validators
{
    /// <summary>
    /// Дуель досі не мала валідатора взагалі: дисципліна приходила будь-яким
    /// рядком, а час — будь-яким, зокрема вчорашнім. Правила ті самі, що й у
    /// виклику команді, — інша тут лише сутність, не зміст.
    /// </summary>
    public class CreateDuelValidator : AbstractValidator<CreateDuelDto>
    {
        private static readonly string[] Formats = { "BO1", "BO3", "BO5" };

        public CreateDuelValidator()
        {
            // Null — відкритий виклик: суперника навмисно не названо.
            RuleFor(x => x.OpponentPlayerId)
                .GreaterThan(0).WithMessage("Вкажіть суперника")
                .When(x => x.OpponentPlayerId.HasValue);

            RuleFor(x => x.Game)
                .NotEmpty().WithMessage("Оберіть дисципліну")
                .Must(Games.IsValid).WithMessage("Оберіть одну з підтримуваних дисциплін");

            RuleFor(x => x.ScheduledAt)
                .NotEmpty().WithMessage("Вкажіть час дуелі")
                .Must(scheduledAt => ScheduleRules.IsInFuture(scheduledAt))
                .WithMessage(ScheduleRules.PastMessage);

            RuleFor(x => x.Format)
                .Must(format => Formats.Contains(format))
                .WithMessage("Формат має бути BO1, BO3 або BO5");

            RuleFor(x => x.Message)
                .MaximumLength(300).WithMessage("Повідомлення не може перевищувати 300 символів");
        }
    }
}
