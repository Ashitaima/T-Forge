using FluentValidation;
using TForge.Common;
using TForge.DTOs;

namespace TForge.Validators
{
    /// <summary>
    /// UpdateTeamOpeningDto навмисно не несе Game — дисципліну на живій
    /// вакансії не міняють. Це ж означає, що тут неможливо звірити Position
    /// і MinRank з дисципліною: її просто нема в DTO. Ту частину перевіряє
    /// TeamOpeningService.UpdateAsync, де Game вже завантажено разом із
    /// самою вакансією. Тут — лише правила, які від дисципліни не залежать,
    /// дзеркало відповідних правил CreateTeamOpeningValidator.
    /// </summary>
    public class UpdateTeamOpeningValidator : AbstractValidator<UpdateTeamOpeningDto>
    {
        public UpdateTeamOpeningValidator()
        {
            RuleFor(x => x.MinUtcOffsetMinutes)
                .Must(TimeZones.IsValid).WithMessage("Невідомий часовий пояс");

            RuleFor(x => x.MaxUtcOffsetMinutes)
                .Must(TimeZones.IsValid).WithMessage("Невідомий часовий пояс");

            // Перевернуте вікно не відкидає нікого — воно просто не має сенсу,
            // і капітан помітить це лише з порожньої стрічки.
            RuleFor(x => x)
                .Must(dto => dto.MinUtcOffsetMinutes == null
                          || dto.MaxUtcOffsetMinutes == null
                          || dto.MinUtcOffsetMinutes <= dto.MaxUtcOffsetMinutes)
                .WithMessage("Початок вікна часових поясів має бути не пізніше за кінець");

            RuleFor(x => x.Pitch)
                .MaximumLength(300).WithMessage("Опис задовгий: максимум 300 символів");
        }
    }
}
