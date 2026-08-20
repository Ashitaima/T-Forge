using FluentValidation;
using TForge.DTOs;

namespace TForge.Validators
{
    public class UpdateUserValidator : AbstractValidator<UpdateUserDto>
    {
        public UpdateUserValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email є обов'язковим")
                .EmailAddress().WithMessage("Некоректний формат email")
                .MaximumLength(100).WithMessage("Email не може перевищувати 100 символів");

            RuleFor(x => x.FirstName)
                .MaximumLength(50).WithMessage("Ім'я не може перевищувати 50 символів");

            RuleFor(x => x.LastName)
                .MaximumLength(50).WithMessage("Прізвище не може перевищувати 50 символів");
        }
    }
}
