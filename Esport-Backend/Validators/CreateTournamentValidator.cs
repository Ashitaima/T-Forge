using FluentValidation;
using Computational_Practice.DTOs;

namespace Computational_Practice.Validators
{
    public class CreateTournamentValidator : AbstractValidator<CreateTournamentDto>
    {
        public CreateTournamentValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Назва турніру є обов'язковою")
                .Length(3, 100).WithMessage("Назва турніру повинна містити від 3 до 100 символів");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Опис не може перевищувати 1000 символів");

            RuleFor(x => x.Game)
                .NotEmpty().WithMessage("Гра є обов'язковою")
                .Length(2, 50).WithMessage("Назва гри повинна містити від 2 до 50 символів");

            RuleFor(x => x.MaxTeams)
                .GreaterThan(1).WithMessage("Максимальна кількість команд повинна бути більше 1")
                .LessThanOrEqualTo(128).WithMessage("Максимальна кількість команд не може перевищувати 128");

            RuleFor(x => x.PrizePool)
                .GreaterThanOrEqualTo(0).WithMessage("Призовий фонд не може бути від'ємним");

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Дата початку є обов'язковою")
                .GreaterThan(DateTime.Today).WithMessage("Дата початку повинна бути в майбутньому");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("Дата завершення є обов'язковою")
                .GreaterThan(x => x.StartDate).WithMessage("Дата завершення повинна бути після дати початку");

            RuleFor(x => x.OrganizerId)
                .GreaterThan(0).WithMessage("ID організатора повинен бути більше 0");
        }
    }
}
