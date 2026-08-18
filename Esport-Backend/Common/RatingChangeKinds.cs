namespace TForge.Common
{
    /// <summary>
    /// Чим є рядок журналу рейтингу. Журнал доповнюваний: виправлений результат
    /// матчу нічого з нього не видаляє, а дописує зворотний запис — так само,
    /// як сторнування у бухгалтерії. Історія лишається перевірною, і графік
    /// чесно показує, що результат виправляли.
    ///
    /// Разом із Revision це і є той дискримінатор, який дозволив розширити
    /// унікальний індекс (TeamId, MatchId) замість того, щоб його прибрати:
    /// саме він робить подвійне нарахування структурно неможливим.
    /// </summary>
    public static class RatingChangeKinds
    {
        /// <summary>Нарахування за результатом матчу.</summary>
        public const string Applied = "Applied";

        /// <summary>Сторнування раніше нарахованого: результат матчу змінили.</summary>
        public const string Reversal = "Reversal";

        public static readonly string[] All = { Applied, Reversal };

        public static bool IsValid(string? kind) => kind != null && All.Contains(kind);
    }
}
