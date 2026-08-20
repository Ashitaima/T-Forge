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

        /// <summary>
        /// Країна зберігається кодом ISO 3166-1 alpha-2 — саме з нього
        /// виводиться прапор. Порожнє значення дозволене (країну можна не
        /// вказувати), але довільний рядок — ні: з нього прапора не зробити,
        /// а форма й так пропонує список.
        /// </summary>
        public static IRuleBuilderOptions<T, string> PlayerCountry<T>(
            this IRuleBuilder<T, string> rule) =>
            rule
                .Must(country => string.IsNullOrEmpty(country) || Countries.IsValid(country))
                .WithMessage("Оберіть країну зі списку");

        /// <summary>
        /// Ігрові теги. Формат вирішує Common/GameIdFormats.cs — тут лише
        /// повідомлення, бо саме правило потрібне ще й тестам без FluentValidation.
        /// Порожнє значення дозволене: тег необов'язковий.
        /// </summary>
        public static IRuleBuilderOptions<T, string?> PlayerRiotId<T>(
            this IRuleBuilder<T, string?> rule) =>
            rule
                .Must(GameIdFormats.IsRiotId)
                .WithMessage("Riot ID має вигляд «Ім'я#TAG», наприклад Shroud#EUW");

        public static IRuleBuilderOptions<T, string?> PlayerSteamId<T>(
            this IRuleBuilder<T, string?> rule) =>
            rule
                .Must(GameIdFormats.IsSteamId64)
                .WithMessage("SteamID64 — це 17 цифр, що починаються з 7656119");

        public static IRuleBuilderOptions<T, string?> PlayerBattleTag<T>(
            this IRuleBuilder<T, string?> rule) =>
            rule
                .Must(GameIdFormats.IsBattleTag)
                .WithMessage("BattleTag має вигляд «Ім'я#1234»");

        public static IRuleBuilderOptions<T, int> PlayerAge<T>(
            this IRuleBuilder<T, int> rule) =>
            rule
                .GreaterThanOrEqualTo(MinAge).WithMessage($"Вік повинен бути мінімум {MinAge} років")
                .LessThanOrEqualTo(MaxAge).WithMessage($"Вік не може перевищувати {MaxAge} років");
    }
}
