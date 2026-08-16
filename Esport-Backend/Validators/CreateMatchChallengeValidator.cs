using FluentValidation;
using TForge.Common;
using TForge.DTOs;

namespace TForge.Validators
{
    public class CreateMatchChallengeValidator : AbstractValidator<CreateMatchChallengeDto>
    {
        private static readonly string[] Formats = { "BO1", "BO3", "BO5" };

        public CreateMatchChallengeValidator()
        {
            RuleFor(x => x.ChallengerTeamId)
                .GreaterThan(0).WithMessage("Вкажіть команду, яка кидає виклик");

            RuleFor(x => x.OpponentTeamId)
                .GreaterThan(0).WithMessage("Вкажіть команду-суперника")
                .NotEqual(x => x.ChallengerTeamId)
                .WithMessage("Команда не може викликати саму себе");

            // Товариський матч не має турніру, тож дисципліну обирає капітан —
            // але лише з підтримуваного списку.
            RuleFor(x => x.Game)
                .NotEmpty().WithMessage("Оберіть дисципліну")
                .Must(Games.IsValid).WithMessage("Оберіть одну з підтримуваних дисциплін");

            RuleFor(x => x.ProposedAt)
                .GreaterThan(_ => DateTime.UtcNow).WithMessage("Час матчу має бути в майбутньому");

            RuleFor(x => x.Format)
                .Must(format => Formats.Contains(format))
                .WithMessage("Формат має бути BO1, BO3 або BO5");

            RuleFor(x => x.Message)
                .MaximumLength(300).WithMessage("Повідомлення не може перевищувати 300 символів");
        }
    }
}
