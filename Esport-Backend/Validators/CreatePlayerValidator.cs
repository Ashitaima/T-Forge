using FluentValidation;
using TForge.DTOs;

namespace TForge.Validators
{
    public class CreatePlayerValidator : AbstractValidator<CreatePlayerDto>
    {
        public CreatePlayerValidator()
        {
            RuleFor(x => x.Nickname)
                .NotEmpty().WithMessage("Нікнейм є обов'язковим")
                .Length(2, 30).WithMessage("Нікнейм повинен містити від 2 до 30 символів")
                .Matches("^[a-zA-Z0-9_]+$").WithMessage("Нікнейм може містити лише літери, цифри та підкреслення");

            RuleFor(x => x.Position)
                .MaximumLength(50).WithMessage("Позиція не може перевищувати 50 символів")
                .Must(BeValidPosition).WithMessage("Некоректна позиція гравця")
                .When(x => !string.IsNullOrEmpty(x.Position));

            RuleFor(x => x.Country)
                .MaximumLength(100).WithMessage("Країна не може перевищувати 100 символів");

            RuleFor(x => x.Age)
                .GreaterThanOrEqualTo(13).WithMessage("Вік повинен бути мінімум 13 років")
                .LessThanOrEqualTo(50).WithMessage("Вік не може перевищувати 50 років")
                .When(x => x.Age > 0);

            // Зазвичай користувача визначає сервер за токеном
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("ID користувача повинен бути більше 0")
                .When(x => x.UserId.HasValue);

            RuleFor(x => x.TeamId)
                .GreaterThan(0).WithMessage("ID команди повинен бути більше 0")
                .When(x => x.TeamId.HasValue);
        }

        private bool BeValidPosition(string position)
        {
            var validPositions = new[] { "Support", "ADC", "Mid", "Jungle", "Top", "IGL", "Entry", "Lurker", "AWPer", "Rifler" };
            return validPositions.Contains(position);
        }
    }
}
