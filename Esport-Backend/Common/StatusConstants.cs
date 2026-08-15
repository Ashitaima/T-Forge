namespace TForge.Common
{
    /// <summary>
    /// Канонічні статуси матчу. Значення повинні збігатися з тими, що використовує фронтенд.
    /// </summary>
    public static class MatchStatus
    {
        public const string Scheduled = "Scheduled";
        public const string InProgress = "InProgress";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
        public const string Postponed = "Postponed";

        public static readonly string[] All =
        {
            Scheduled, InProgress, Completed, Cancelled, Postponed
        };

        public static bool IsValid(string? status) => status != null && All.Contains(status);
    }

    /// <summary>
    /// Типи матчів. Раунди сітки називаються за кількістю команд у раунді.
    /// </summary>
    public static class MatchTypes
    {
        public const string GroupStage = "GroupStage";
        public const string PlayIn = "PlayIn";
        public const string RoundOf32 = "RoundOf32";
        public const string RoundOf16 = "RoundOf16";
        public const string QuarterFinal = "QuarterFinal";
        public const string SemiFinal = "SemiFinal";
        public const string Final = "Final";
        public const string ThirdPlace = "ThirdPlace";

        public static readonly string[] All =
        {
            GroupStage, PlayIn, RoundOf32, RoundOf16, QuarterFinal, SemiFinal, Final, ThirdPlace
        };

        public static bool IsValid(string? matchType) => matchType != null && All.Contains(matchType);

        /// <summary>
        /// Назва раунду за кількістю команд, що в ньому стартують.
        /// </summary>
        public static string ForRoundSize(int teamsInRound) => teamsInRound switch
        {
            2 => Final,
            4 => SemiFinal,
            8 => QuarterFinal,
            16 => RoundOf16,
            32 => RoundOf32,
            _ => PlayIn
        };
    }

    /// <summary>
    /// Канонічні статуси турніру. Значення повинні збігатися з тими, що використовує фронтенд.
    /// </summary>
    public static class TournamentStatus
    {
        public const string Registration = "Registration";
        public const string InProgress = "InProgress";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All =
        {
            Registration, InProgress, Completed, Cancelled
        };

        public static bool IsValid(string? status) => status != null && All.Contains(status);
    }

    /// <summary>
    /// Результат з погляду однієї сторони. Використовується і для серії команди,
    /// і для рядка журналу матчів гравця, тому рядкові значення визначені один раз.
    /// </summary>
    public static class ResultType
    {
        public const string Win = "Win";
        public const string Loss = "Loss";
        public const string Pending = "Pending";
    }

    /// <summary>
    /// Напрям запиту на членство: хто його ініціював.
    /// Значення повинні збігатися з тими, що використовує фронтенд.
    /// </summary>
    public static class MembershipRequestDirection
    {
        /// <summary>Капітан запросив гравця — відповідає гравець.</summary>
        public const string Invite = "Invite";

        /// <summary>Гравець подав заявку — відповідає капітан.</summary>
        public const string Application = "Application";

        public static readonly string[] All = { Invite, Application };

        public static bool IsValid(string? direction) => direction != null && All.Contains(direction);
    }

    /// <summary>
    /// Статуси запиту на членство. Accepted, Declined і Cancelled — термінальні.
    /// </summary>
    public static class MembershipRequestStatus
    {
        public const string Pending = "Pending";
        public const string Accepted = "Accepted";
        public const string Declined = "Declined";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All = { Pending, Accepted, Declined, Cancelled };

        public static bool IsValid(string? status) => status != null && All.Contains(status);
    }
}
