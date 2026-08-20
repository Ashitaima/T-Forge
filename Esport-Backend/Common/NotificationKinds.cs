namespace TForge.Common
{
    /// <summary>
    /// Види сповіщень. Кожна пара «джерело + напрям» дає два різні види —
    /// той, що чекає на відповідь, і той, що повідомляє про неї. Це різні
    /// речення для різних людей, тож і ключі різні: інакше різниця переїхала б
    /// у побудову рядка заголовка, де її вже не перевіриш тестом.
    ///
    /// Підписи живуть на фронтенді, як у Games і RatingTiers.
    /// </summary>
    public static class NotificationKinds
    {
        public const string MembershipInviteReceived = "MembershipInviteReceived";
        public const string MembershipInviteAnswered = "MembershipInviteAnswered";
        public const string MembershipApplicationReceived = "MembershipApplicationReceived";
        public const string MembershipApplicationAnswered = "MembershipApplicationAnswered";
        public const string ChallengeReceived = "ChallengeReceived";
        public const string ChallengeAnswered = "ChallengeAnswered";
        public const string TournamentInviteReceived = "TournamentInviteReceived";
        public const string TournamentInviteAnswered = "TournamentInviteAnswered";
        public const string TournamentApplicationReceived = "TournamentApplicationReceived";
        public const string TournamentApplicationAnswered = "TournamentApplicationAnswered";

        public static readonly string[] All =
        {
            MembershipInviteReceived, MembershipInviteAnswered,
            MembershipApplicationReceived, MembershipApplicationAnswered,
            ChallengeReceived, ChallengeAnswered,
            TournamentInviteReceived, TournamentInviteAnswered,
            TournamentApplicationReceived, TournamentApplicationAnswered
        };

        public static bool IsValid(string? kind) => kind != null && All.Contains(kind);
    }
}
