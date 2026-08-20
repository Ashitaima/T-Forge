using FluentValidation;
using TForge.Common;
using TForge.DTOs;

namespace TForge.Validators
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

            // Список, а не вільний текст: «Europe», «EU» і «Європа» інакше
            // ставали трьома різними регіонами, за якими нічого не згрупувати.
            RuleFor(x => x.Region)
                .NotEmpty().WithMessage("Регіон є обов'язковим")
                .Must(Regions.IsValid).WithMessage("Оберіть регіон зі списку");

            // Зазвичай капітана визначає сервер за токеном; перевіряємо лише явно передане значення
            RuleFor(x => x.CaptainId)
                .GreaterThan(0).WithMessage("ID капітана повинен бути більше 0")
                .When(x => x.CaptainId.HasValue);
        }
    }
}
