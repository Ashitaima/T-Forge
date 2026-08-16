using FluentValidation;
using TForge.DTOs;

namespace TForge.Validators
{
    /// <summary>
    /// Правила збігаються з RegisterValidator для облікових полів і з
    /// CreatePlayerValidator для ігрових — адміністратор створює те саме,
    /// що користувач створив би сам, лише одним кроком.
    /// </summary>
    public class CreateFullPlayerValidator : AbstractValidator<CreateFullPlayerDto>
    {
        public CreateFullPlayerValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Ім'я користувача є обов'язковим")
                .MinimumLength(3).WithMessage("Ім'я користувача повинно містити щонайменше 3 символи")
                .MaximumLength(50).WithMessage("Ім'я користувача не може перевищувати 50 символів")
                .Matches("^[a-zA-Z0-9_]+$")
                .WithMessage("Ім'я користувача може містити лише букви, цифри та підкреслення");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email є обов'язковим")
                .EmailAddress().WithMessage("Некоректний формат email")
                .MaximumLength(100).WithMessage("Email не може перевищувати 100 символів");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль є обов'язковим")
                .MinimumLength(8).WithMessage("Пароль повинен містити щонайменше 8 символів")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).*$")
                .WithMessage("Пароль повинен містити щонайменше одну велику букву, одну малу букву та одну цифру");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Ім'я є обов'язковим")
                .MaximumLength(50).WithMessage("Ім'я не може перевищувати 50 символів");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Прізвище є обов'язковим")
                .MaximumLength(50).WithMessage("Прізвище не може перевищувати 50 символів");

            RuleFor(x => x.Nickname).PlayerNickname();

            RuleFor(x => x.Position)
                .PlayerPosition()
                .When(x => !string.IsNullOrEmpty(x.Position));

            RuleFor(x => x.Country).PlayerCountry();

            RuleFor(x => x.Age).PlayerAge();
        }
    }
}
