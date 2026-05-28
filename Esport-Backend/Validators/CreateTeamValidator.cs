using FluentValidation;
using Computational_Practice.DTOs;

namespace Computational_Practice.Validators
{
    public class CreateTeamValidator : AbstractValidator<CreateTeamDto>
    {
        public CreateTeamValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Назва команди є обов'язковою")
                .Length(2, 50).WithMessage("Назва команди повинна містити від 2 до 50 символів")
                .Matches("^[a-zA-Z0-9 _-]+$").WithMessage("Назва команди може містити лише літери, цифри, пробіли, підкреслення та дефіси");

            RuleFor(x => x.Tag)
                .NotEmpty().WithMessage("Тег команди є обов'язковим")
                .Length(2, 10).WithMessage("Тег команди повинен містити від 2 до 10 символів")
                .Matches("^[a-zA-Z0-9]+$").WithMessage("Тег команди може містити лише літери та цифри");

            RuleFor(x => x.Description)
                .MaximumLength(300).WithMessage("Опис не може перевищувати 300 символів");

            RuleFor(x => x.Region)
                .NotEmpty().WithMessage("Регіон є обов'язковим")
                .MaximumLength(100).WithMessage("Регіон не може перевищувати 100 символів");

            RuleFor(x => x.CaptainId)
                .GreaterThan(0).WithMessage("ID капітана повинен бути більше 0");
        }
    }
}
