using FluentValidation;
using TForge.Common;
using TForge.DTOs;

namespace TForge.Validators
{
    public class LoginValidator : AbstractValidator<LoginDto>
    {
        public LoginValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Ім'я користувача є обов'язковим")
                .MinimumLength(3).WithMessage("Ім'я користувача повинно містити щонайменше 3 символи")
                .MaximumLength(50).WithMessage("Ім'я користувача не може перевищувати 50 символів");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль є обов'язковим")
                .MinimumLength(6).WithMessage("Пароль повинен містити щонайменше 6 символів");
        }
    }

    public class RegisterValidator : AbstractValidator<RegisterDto>
    {
        public RegisterValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Ім'я користувача є обов'язковим")
                .MinimumLength(3).WithMessage("Ім'я користувача повинно містити щонайменше 3 символи")
                .MaximumLength(50).WithMessage("Ім'я користувача не може перевищувати 50 символів")
                .Matches("^[a-zA-Z0-9_]+$").WithMessage("Ім'я користувача може містити лише букви, цифри та підкреслення");

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
                .MaximumLength(50).WithMessage("Ім'я не може перевищувати 50 символів");

            RuleFor(x => x.LastName)
                .MaximumLength(50).WithMessage("Прізвище не може перевищувати 50 символів");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Роль є обов'язковою")
                .Must(UserRoles.IsSelfService)
                .WithMessage("Зареєструватися можна лише як гравець або організатор");

            // Профіль гравця створюється при кожній реєстрації — навіть коли
            // просять роль організатора, бо ту роль видає лише адміністратор,
            // а до того акаунт живе гравцем. Тож нікнейм потрібен завжди.
            RuleFor(x => x.Nickname).PlayerNickname();
        }
    }
}
