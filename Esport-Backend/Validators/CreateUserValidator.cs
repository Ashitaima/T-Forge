using FluentValidation;
using Computational_Practice.DTOs;

namespace Computational_Practice.Validators
{
    public class CreateUserValidator : AbstractValidator<CreateUserDto>
    {
        public CreateUserValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Ім'я користувача є обов'язковим")
                .Length(3, 50).WithMessage("Ім'я користувача повинно містити від 3 до 50 символів")
                .Matches("^[a-zA-Z0-9_]+$").WithMessage("Ім'я користувача може містити лише літери, цифри та підкреслення");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email є обов'язковим")
                .EmailAddress().WithMessage("Некоректний формат email")
                .MaximumLength(100).WithMessage("Email не може перевищувати 100 символів");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль є обов'язковим")
                .MinimumLength(6).WithMessage("Пароль повинен містити мінімум 6 символів")
                .MaximumLength(100).WithMessage("Пароль не може перевищувати 100 символів");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Ім'я є обов'язковим")
                .Length(1, 50).WithMessage("Ім'я повинно містити від 1 до 50 символів");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Прізвище є обов'язковим")
                .Length(1, 50).WithMessage("Прізвище повинно містити від 1 до 50 символів");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Роль користувача є обов'язковою")
                .Must(BeValidRole).WithMessage("Некоректна роль користувача");
        }

        private bool BeValidRole(string role)
        {
            var validRoles = new[] { "Admin", "Organizer", "Player", "User" };
            return validRoles.Contains(role);
        }
    }
}
