namespace TForge.Common
{
    /// <summary>
    /// Стан вакансії в складі команди. Filled ставить шлях прийняття запиту,
    /// Cancelled — капітан; Open лишається, доки вакансію не закрили.
    /// </summary>
    public static class TeamOpeningStatus
    {
        public const string Open = "Open";
        public const string Filled = "Filled";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All = { Open, Filled, Cancelled };

        public static bool IsValid(string? status) => status != null && All.Contains(status);
    }
}
