namespace TForge.Common
{
    /// <summary>
    /// Чи можна ще призначити гру на цей час. Чиста функція, як інші правила
    /// в цій теці — валідатори читають DTO, рішення ухвалює цей клас.
    ///
    /// Правило одне на всі три сутності, які щось планують: матч, виклик
    /// команді та дуель. Доти воно існувало в трьох станах — виклик звірявся
    /// з UtcNow, дуель не звірялася ні з чим, а матч порівнював уже переведене
    /// в UTC значення з <c>DateTime.Now</c>, тобто з місцевим часом. Останнє —
    /// не дрібниця: в UTC+3 усе, що ближче ніж за три години, зараховувалося
    /// як «у минулому», і матч на сьогоднішній вечір створити було не можна.
    /// </summary>
    public static class ScheduleRules
    {
        /// <summary>
        /// Запас на розходження годинників клієнта й сервера. Без нього час
        /// «через хвилину» відхилявся б, поки запит летить.
        /// </summary>
        public static readonly TimeSpan ClockTolerance = TimeSpan.FromMinutes(2);

        /// <summary>
        /// <paramref name="scheduledAt"/> приходить із тіла запиту, тож може
        /// мати будь-який Kind — приводимо його тим самим правилом, що й
        /// UtcDateTimeConverter, інакше порівняння знову зіставляло б місцевий
        /// час із UTC.
        /// </summary>
        public static bool IsInFuture(DateTime scheduledAt, DateTime nowUtc) =>
            UtcDateTimeConverter.ToUtc(scheduledAt) > nowUtc - ClockTolerance;

        public static bool IsInFuture(DateTime scheduledAt) =>
            IsInFuture(scheduledAt, DateTime.UtcNow);

        /// <summary>Спільний текст помилки — щоб три форми не розійшлися.</summary>
        public const string PastMessage = "Час має бути в майбутньому — минулою датою гру не призначають";
    }
}
