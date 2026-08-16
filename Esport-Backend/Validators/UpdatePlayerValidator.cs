using FluentValidation;
using TForge.DTOs;

namespace TForge.Validators
{
    public class UpdatePlayerValidator : AbstractValidator<UpdatePlayerDto>
    {
        public UpdatePlayerValidator()
        {
            RuleFor(x => x.Nickname).PlayerNickname();

            RuleFor(x => x.Position)
                .PlayerPosition()
                .When(x => !string.IsNullOrEmpty(x.Position));

            RuleFor(x => x.Country).PlayerCountry();

            RuleFor(x => x.Age)
                .PlayerAge()
                .When(x => x.Age > 0);
        }
    }
}
