namespace TForge.Common
{
    /// <summary>
    /// Кому адресовано рядок запиту й чи чекає він дії. Чиста функція над
    /// примітивами — як FriendlyMatchPolicy і TournamentOwnershipPolicy.
    /// Сервіс читає дані, рішення ухвалює цей клас.
    ///
    /// Три таблиці — TeamMembershipRequest, MatchChallenge та
    /// TournamentInvitation — мають майже однакову форму (Status,
    /// InitiatedByUserId, CreatedAt, RespondedAt), тож одне правило накриває всі.
    /// </summary>
    public static class NotificationAddressing
    {
        public static class Sources
        {
            public const string Membership = "Membership";
            public const string Challenge = "Challenge";
            public const string Tournament = "Tournament";
        }

        /// <summary>Кому показати рядок і чи можна на нього відповісти прямо зараз.</summary>
        public record Audience(int UserId, bool IsActionable);

        /// <summary>
        /// Поки рядок відкритий — він чекає на відповідача. Щойно закритий —
        /// повідомляє ініціатора: питав саме він, тож відповідь потрібна йому.
        /// RespondedByUserId аудиторією не буває ніколи — той, хто відповів,
        /// уже знає, що зробив.
        ///
        /// Скасований рядок не адресовано нікому: скасовує його ініціатор,
        /// а відповідачеві більше нема на що відповідати.
        /// </summary>
        public static Audience? For(
            string source,
            string? direction,
            string status,
            int initiatedByUserId,
            int responderCandidateUserId)
        {
            if (IsPending(source, status))
            {
                return new Audience(responderCandidateUserId, IsActionable: true);
            }

            if (!IsAnswered(source, status))
            {
                return null;
            }

            // Ініціатор відповів сам собі (наприклад, за нього це зробив адмін
            // з того самого акаунта) — повідомляти нема про що.
            if (initiatedByUserId == responderCandidateUserId)
            {
                return null;
            }

            return new Audience(initiatedByUserId, IsActionable: false);
        }

        /// <summary>
        /// Ключ виду складається з джерела, напряму й того, чекає рядок
        /// відповіді чи вже повідомляє про неї.
        /// </summary>
        public static string Kind(string source, string? direction, bool isActionable) =>
            (source, direction, isActionable) switch
            {
                (Sources.Membership, MembershipRequestDirection.Invite, true) =>
                    NotificationKinds.MembershipInviteReceived,
                (Sources.Membership, MembershipRequestDirection.Invite, false) =>
                    NotificationKinds.MembershipInviteAnswered,
                (Sources.Membership, _, true) =>
                    NotificationKinds.MembershipApplicationReceived,
                (Sources.Membership, _, false) =>
                    NotificationKinds.MembershipApplicationAnswered,

                (Sources.Tournament, TournamentInvitationDirection.Invite, true) =>
                    NotificationKinds.TournamentInviteReceived,
                (Sources.Tournament, TournamentInvitationDirection.Invite, false) =>
                    NotificationKinds.TournamentInviteAnswered,
                (Sources.Tournament, _, true) =>
                    NotificationKinds.TournamentApplicationReceived,
                (Sources.Tournament, _, false) =>
                    NotificationKinds.TournamentApplicationAnswered,

                (_, _, true) => NotificationKinds.ChallengeReceived,
                (_, _, false) => NotificationKinds.ChallengeAnswered
            };

        private static bool IsPending(string source, string status) => source switch
        {
            Sources.Challenge => status == MatchChallengeStatus.Pending,
            Sources.Tournament => status == TournamentInvitationStatus.Pending,
            _ => status == MembershipRequestStatus.Pending
        };

        private static bool IsAnswered(string source, string status) => source switch
        {
            Sources.Challenge =>
                status == MatchChallengeStatus.Accepted || status == MatchChallengeStatus.Declined,
            Sources.Tournament =>
                status == TournamentInvitationStatus.Accepted
                || status == TournamentInvitationStatus.Declined,
            _ =>
                status == MembershipRequestStatus.Accepted
                || status == MembershipRequestStatus.Declined
        };
    }
}
