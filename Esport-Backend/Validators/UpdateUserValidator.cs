using FluentValidation;
using Computational_Practice.DTOs;

namespace Computational_Practice.Validators
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
                .NotEmpty().WithMessage("Ім'я є обов'язковим")
                .Length(1, 50).WithMessage("Ім'я повинно містити від 1 до 50 символів");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Прізвище є обов'язковим")
                .Length(1, 50).WithMessage("Прізвище повинно містити від 1 до 50 символів");
        }
    }
}
