namespace TForge.Common
{
    /// <summary>
    /// Уся арифметика рейтингу. Чистий статичний клас без EF і сервісів —
    /// та сама форма, що в TeamRecordCalculator і PlayerRecordCalculator, і саме
    /// вона дозволяє перевіряти рейтинг юніт-тестами без бази. Сервіс лише читає
    /// й пише рядки, а рахує все тут.
    /// </summary>
    public static class EloCalculator
    {
        /// <summary>Рейтинг, з якого починає команда або гравець без історії.</summary>
        public const int BaseRating = 1000;

        /// <summary>
        /// Нижня межа. Без неї смуга поразок опускає рейтинг до майже нуля,
        /// з якого вже не видно жодного прогресу.
        /// </summary>
        public const int FloorRating = 100;

        /// <summary>Скільки перших матчів рахуються з подвійним K.</summary>
        public const int ProvisionalMatches = 5;

        /// <summary>K для звичайного турнірного матчу.</summary>
        public const int TournamentK = 24;

        /// <summary>
        /// K для матчу, що роздає нагороди. Фінал має рухати число помітно
        /// сильніше за груповий матч — інакше титул нічого не важить.
        /// </summary>
        public const int DecisiveK = 40;

        /// <summary>
        /// Товариський матч не рейтингується взагалі: два капітани можуть
        /// викликати одне одного скільки завгодно, і це не має ставати
        /// способом накрутити драбину.
        /// </summary>
        public static bool IsRated(int? tournamentId, string? status, int? winnerTeamId) =>
            tournamentId != null
            && status == MatchStatus.Completed
            && winnerTeamId != null;

        /// <summary>
        /// K обирається за важливістю матчу й подвоюється, поки суб'єкт ще
        /// «пристрілюється»: нова команда доходить до свого рівня за кілька
        /// ігор, а не за тридцять.
        /// </summary>
        public static int KFactor(string? matchType, int matchesRated)
        {
            var k = matchType == MatchTypes.Final || matchType == MatchTypes.ThirdPlace
                ? DecisiveK
                : TournamentK;

            return matchesRated < ProvisionalMatches ? k * 2 : k;
        }

        /// <summary>Класичне очікування Ело: 1 / (1 + 10^((суперник - свій) / 400)).</summary>
        public static double ExpectedScore(int selfRating, int opponentRating) =>
            1.0 / (1.0 + Math.Pow(10, (opponentRating - selfRating) / 400.0));

        /// <summary>
        /// Приріст рейтингу за один матч. Нічия сюди не потрапляє: її відсіює
        /// IsRated, бо PlayerRecordCalculator і TeamRecordCalculator теж не
        /// рахують невизначені матчі — інакше драбина розійшлася б із колонкою
        /// відсотка перемог, що стоїть поруч.
        /// </summary>
        public static int Delta(int selfRating, int opponentRating, bool won, int kFactor)
        {
            var actual = won ? 1.0 : 0.0;
            return (int)Math.Round(
                kFactor * (actual - ExpectedScore(selfRating, opponentRating)),
                MidpointRounding.AwayFromZero);
        }

        /// <summary>Новий рейтинг із урахуванням підлоги.</summary>
        public static int Apply(int rating, int delta) => Math.Max(FloorRating, rating + delta);

        /// <summary>Ліга для рейтингу — щоб виклик ішов через один калькулятор.</summary>
        public static string Tier(int rating) => RatingTiers.ForRating(rating);
    }
}
