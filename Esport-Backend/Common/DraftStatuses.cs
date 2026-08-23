namespace TForge.Common
{
    /// <summary>
    /// Стан драфту. Значення мусять збігатися з
    /// Esport-Frontend/src/constants/draft.ts — інакше клієнт малює один стан,
    /// а сервер тримає інший.
    /// </summary>
    public static class DraftStatuses
    {
        /// <summary>Кроки створено, але хід ще не пішов.</summary>
        public const string Pending = "Pending";

        /// <summary>Іде: рівно один крок чекає на команду, таймер цокає.</summary>
        public const string InProgress = "InProgress";

        public const string Completed = "Completed";

        /// <summary>Скасовано організатором чи капітаном; матчу не заважає.</summary>
        public const string Cancelled = "Cancelled";

        public static readonly string[] All = { Pending, InProgress, Completed, Cancelled };

        public static bool IsValid(string? status) => status != null && All.Contains(status);
    }
}
