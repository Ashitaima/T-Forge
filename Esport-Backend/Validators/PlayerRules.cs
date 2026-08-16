using FluentValidation;
using TForge.Common;

namespace TForge.Validators
{
    /// <summary>
    /// Правила для полів гравця, спільні для реєстрації, створення та редагування.
    /// Раніше кожен валідатор мав власну копію — і вони встигли розійтися
    /// (нікнейм від 2 чи від 3 символів, різні межі віку). Одне визначення
    /// гарантує, що форма й сервер очікують те саме.
    /// </summary>
    public static class PlayerRules
    {
        public const int NicknameMinLength = 3;
        public const int NicknameMaxLength = 30;
        public const int MinAge = 13;
        public const int MaxAge = 50;

        public static IRuleBuilderOptions<T, string> PlayerNickname<T>(
            this IRuleBuilder<T, string> rule) =>
            rule
                .NotEmpty().WithMessage("Нікнейм є обов'язковим")
                .Length(NicknameMinLength, NicknameMaxLength)
                .WithMessage($"Нікнейм повинен містити від {NicknameMinLength} до {NicknameMaxLength} символів")
                .Matches("^[a-zA-Z0-9_]+$")
                .WithMessage("Нікнейм може містити лише літери, цифри та підкреслення");

        public static IRuleBuilderOptions<T, string> PlayerPosition<T>(
            this IRuleBuilder<T, string> rule) =>
            rule
                .Must(PlayerPositions.IsValid).WithMessage("Некоректна позиція гравця");

        public static IRuleBuilderOptions<T, string> PlayerCountry<T>(
            this IRuleBuilder<T, string> rule) =>
            rule
                .MaximumLength(100).WithMessage("Країна не може перевищувати 100 символів");

        public static IRuleBuilderOptions<T, int> PlayerAge<T>(
            this IRuleBuilder<T, int> rule) =>
            rule
                .GreaterThanOrEqualTo(MinAge).WithMessage($"Вік повинен бути мінімум {MinAge} років")
                .LessThanOrEqualTo(MaxAge).WithMessage($"Вік не може перевищувати {MaxAge} років");
    }
}
