using System.Text.RegularExpressions;

namespace TForge.Common
{
    /// <summary>
    /// Формати ігрових ідентифікаторів гравця — чисті функції, як і решта
    /// правил у Common.
    ///
    /// Кожна платформа має власний формат, і жоден із них не є довільним
    /// рядком: Riot ID — це «Ім'я#TAG», SteamID64 — рівно 17 цифр, що
    /// починаються з 7656119 (це база 76561197960265728 плюс номер акаунта),
    /// BattleTag — «Ім'я#1234». Перевіряти їх варто саме тут: неправильний тег
    /// не помітно доти, доки за ним не спробують когось знайти.
    ///
    /// Порожнє значення валідне скрізь — тег необов'язковий, і гравець без
    /// акаунта в Riot не мусить його вигадувати.
    ///
    /// Дзеркало на клієнті — Esport-Frontend/src/constants/gameIds.ts, той
    /// самий поділ, що у Games та Countries: тут формат, там підписи.
    /// </summary>
    public static class GameIdFormats
    {
        /// <summary>Спільна межа для колонок — жоден із форматів і близько її не сягає.</summary>
        public const int MaxLength = 64;

        // Ім'я до решітки — 3..16 символів без самої решітки; тег — 2..5
        // букв або цифр. Пробіли всередині імені Riot дозволяє.
        private static readonly Regex RiotIdPattern =
            new(@"^[^#\s][^#]{1,14}[^#\s]#[A-Za-z0-9]{2,5}$", RegexOptions.Compiled);

        private static readonly Regex SteamId64Pattern =
            new(@"^7656119\d{10}$", RegexOptions.Compiled);

        private static readonly Regex BattleTagPattern =
            new(@"^[A-Za-z][A-Za-z0-9]{2,11}#\d{4,5}$", RegexOptions.Compiled);

        public static bool IsRiotId(string? value) => IsEmptyOrMatch(RiotIdPattern, value);

        public static bool IsSteamId64(string? value) => IsEmptyOrMatch(SteamId64Pattern, value);

        public static bool IsBattleTag(string? value) => IsEmptyOrMatch(BattleTagPattern, value);

        /// <summary>
        /// Канонічна сторінка профілю Steam. Єдиний із трьох ідентифікаторів,
        /// що дає однозначне посилання: Riot ID і BattleTag без регіону та
        /// дисципліни нікуди не ведуть, тож їх показуємо просто текстом.
        /// </summary>
        public static string? SteamProfileUrl(string? steamId64) =>
            IsSteamId64(steamId64) && !string.IsNullOrWhiteSpace(steamId64)
                ? $"https://steamcommunity.com/profiles/{steamId64.Trim()}"
                : null;

        /// <summary>
        /// Те, що потрапляє в колонку. Перевірка порівнює вже обрізаний рядок,
        /// тож зберігати неохайний — означає розійтися з власним правилом:
        /// « Shroud#EUW » пройшло б валідацію, а знайти за ним нікого.
        /// Порожнє стає null: «не вказав» — це відсутність, а не порожній рядок.
        /// </summary>
        public static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static bool IsEmptyOrMatch(Regex pattern, string? value) =>
            string.IsNullOrWhiteSpace(value) || pattern.IsMatch(value.Trim());
    }
}
