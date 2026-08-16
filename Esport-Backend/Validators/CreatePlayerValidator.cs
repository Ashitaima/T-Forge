using FluentValidation;
using TForge.DTOs;

namespace TForge.Validators
{
    public class CreatePlayerValidator : AbstractValidator<CreatePlayerDto>
    {
        public CreatePlayerValidator()
        {
            RuleFor(x => x.Nickname).PlayerNickname();

            RuleFor(x => x.Position)
                .PlayerPosition()
                .When(x => !string.IsNullOrEmpty(x.Position));

            RuleFor(x => x.Country).PlayerCountry();

            RuleFor(x => x.Age)
                .PlayerAge()
                .When(x => x.Age > 0);

            // Зазвичай користувача визначає сервер за токеном
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("ID користувача повинен бути більше 0")
                .When(x => x.UserId.HasValue);
        }
    }
}
