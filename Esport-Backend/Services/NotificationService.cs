using Microsoft.EntityFrameworkCore;
using TForge.Common;
using TForge.Data.Interfaces;
using TForge.DTOs;
using TForge.Exceptions;
using TForge.Services.Interfaces;

namespace TForge.Services
{
    /// <summary>
    /// Сповіщення виводяться із запитів, а не зберігаються окремо: інакше
    /// з'явилося б друге джерело правди про те, що вже записано в
    /// TeamMembershipRequest, MatchChallenge й TournamentInvitation. Скасований
    /// виклик перестає бути сповіщенням тієї ж миті, коли його скасували.
    ///
    /// Стеля цього рішення названа прямо: сповістити можна лише про ці три
    /// потоки. «Ваша команда виграла турнір» тут не виражається — для цього
    /// потрібна таблиця подій.
    /// </summary>
    public class NotificationService : INotificationService
    {
        /// <summary>Дзвінок — це стрічка останніх подій, а не сторінковий список.</summary>
        private const int MaxItems = 50;

        private readonly IUnitOfWork _unitOfWork;

        public NotificationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<NotificationDto>> GetForUserAsync(int userId)
        {
            var seenAt = await GetSeenAtAsync(userId);
            var rows = await CollectAsync(userId, seenAt);

            return rows.OrderByDescending(n => n.CreatedAt).Take(MaxItems).ToList();
        }

        /// <summary>
        /// Рахує через ту саму збірку, а не окремим COUNT у SQL. Інакше правило
        /// адресування довелося б виразити вдруге — вже мовою запиту, — і саме
        /// ці дві копії з часом і розійшлися б. Ті умови, що перекладаються
        /// (користувач є капітаном, гравцем, організатором або ініціатором),
        /// і так відпрацьовують у базі й лишають небагато рядків.
        /// </summary>
        public async Task<int> GetUnreadCountAsync(int userId)
        {
            var seenAt = await GetSeenAtAsync(userId);
            var rows = await CollectAsync(userId, seenAt);

            return rows.Count(n => n.IsUnread);
        }

        public async Task MarkSeenAsync(int userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId)
                ?? throw new EntityNotFoundException("User", userId);

            user.NotificationsSeenAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task<DateTime?> GetSeenAtAsync(int userId) =>
            await _unitOfWork.Users.GetQueryable()
                .Where(u => u.Id == userId)
                .Select(u => u.NotificationsSeenAt)
                .FirstOrDefaultAsync();

        private async Task<List<NotificationDto>> CollectAsync(int userId, DateTime? seenAt)
        {
            var result = new List<NotificationDto>();

            // --- Запити на членство в команді ---
            var memberships = await _unitOfWork.MembershipRequests.GetQueryable()
                .Where(r => r.Team.CaptainId == userId
                            || r.Player.UserId == userId
                            || r.InitiatedByUserId == userId)
                .Select(r => new
                {
                    r.Direction,
                    r.Status,
                    r.InitiatedByUserId,
                    r.CreatedAt,
                    r.RespondedAt,
                    r.TeamId,
                    TeamName = r.Team.Name,
                    CaptainUserId = r.Team.CaptainId,
                    PlayerUserId = r.Player.UserId,
                    PlayerNickname = r.Player.Nickname
                })
                .ToListAsync();

            foreach (var row in memberships)
            {
                var responder = row.Direction == MembershipRequestDirection.Invite
                    ? row.PlayerUserId
                    : row.CaptainUserId;

                var audience = NotificationAddressing.For(
                    NotificationAddressing.Sources.Membership,
                    row.Direction, row.Status, row.InitiatedByUserId, responder);

                if (audience == null || audience.UserId != userId)
                {
                    continue;
                }

                var kind = NotificationAddressing.Kind(
                    NotificationAddressing.Sources.Membership, row.Direction, audience.IsActionable);
                var at = row.RespondedAt ?? row.CreatedAt;

                result.Add(new NotificationDto
                {
                    Kind = kind,
                    Title = MembershipTitle(kind, row.TeamName, row.PlayerNickname, row.Status),
                    Link = $"/teams/{row.TeamId}",
                    CreatedAt = at,
                    IsUnread = IsUnread(at, seenAt),
                    IsActionable = audience.IsActionable
                });
            }

            // --- Виклики на товариський матч ---
            var challenges = await _unitOfWork.MatchChallenges.GetQueryable()
                .Where(c => c.ChallengerTeam.CaptainId == userId
                            || (c.OpponentTeam != null && c.OpponentTeam.CaptainId == userId))
                .Select(c => new
                {
                    c.Status,
                    c.InitiatedByUserId,
                    c.CreatedAt,
                    c.RespondedAt,
                    c.Message,
                    c.OpponentTeamId,
                    OpponentCaptainUserId = (int?)c.OpponentTeam!.CaptainId,
                    ChallengerName = c.ChallengerTeam.Name,
                    OpponentName = c.OpponentTeam!.Name
                })
                .ToListAsync();

            foreach (var row in challenges)
            {
                // Відкритий виклик поки що не адресовано нікому: команди-
                // суперника ще немає, тож і капітана, якого можна сповістити,
                // теж. Щойно виклик приймуть, рядок стає звичайним адресним.
                if (row.OpponentCaptainUserId is not int opponentCaptainUserId)
                {
                    continue;
                }

                var audience = NotificationAddressing.For(
                    NotificationAddressing.Sources.Challenge,
                    null, row.Status, row.InitiatedByUserId, opponentCaptainUserId);

                if (audience == null || audience.UserId != userId)
                {
                    continue;
                }

                var kind = NotificationAddressing.Kind(
                    NotificationAddressing.Sources.Challenge, null, audience.IsActionable);
                var at = row.RespondedAt ?? row.CreatedAt;

                result.Add(new NotificationDto
                {
                    Kind = kind,
                    Title = audience.IsActionable
                        ? $"«{row.ChallengerName}» викликає вас на товариський матч"
                        : $"«{row.OpponentName}» {AnswerWord(row.Status)} ваш виклик",
                    Body = string.IsNullOrWhiteSpace(row.Message) ? null : row.Message,
                    Link = $"/teams/{row.OpponentTeamId}",
                    CreatedAt = at,
                    IsUnread = IsUnread(at, seenAt),
                    IsActionable = audience.IsActionable
                });
            }

            // --- Запрошення та заявки на турнір ---
            var invitations = await _unitOfWork.TournamentInvitations.GetQueryable()
                .Where(i => i.Team.CaptainId == userId
                            || i.Tournament.OrganizerId == userId
                            || i.InitiatedByUserId == userId)
                .Select(i => new
                {
                    i.Direction,
                    i.Status,
                    i.InitiatedByUserId,
                    i.CreatedAt,
                    i.RespondedAt,
                    i.Message,
                    i.TournamentId,
                    CaptainUserId = i.Team.CaptainId,
                    OrganizerUserId = i.Tournament.OrganizerId,
                    TeamName = i.Team.Name,
                    TournamentName = i.Tournament.Name
                })
                .ToListAsync();

            foreach (var row in invitations)
            {
                var responder = row.Direction == TournamentInvitationDirection.Invite
                    ? row.CaptainUserId
                    : row.OrganizerUserId;

                var audience = NotificationAddressing.For(
                    NotificationAddressing.Sources.Tournament,
                    row.Direction, row.Status, row.InitiatedByUserId, responder);

                if (audience == null || audience.UserId != userId)
                {
                    continue;
                }

                var kind = NotificationAddressing.Kind(
                    NotificationAddressing.Sources.Tournament, row.Direction, audience.IsActionable);
                var at = row.RespondedAt ?? row.CreatedAt;

                result.Add(new NotificationDto
                {
                    Kind = kind,
                    Title = TournamentTitle(kind, row.TournamentName, row.TeamName, row.Status),
                    Body = string.IsNullOrWhiteSpace(row.Message) ? null : row.Message,
                    Link = $"/tournaments/{row.TournamentId}",
                    CreatedAt = at,
                    IsUnread = IsUnread(at, seenAt),
                    IsActionable = audience.IsActionable
                });
            }

            return result;
        }

        /// <summary>Нічого не бачив — отже, все нове.</summary>
        private static bool IsUnread(DateTime at, DateTime? seenAt) => seenAt == null || at > seenAt;

        /// <summary>
        /// Усі три таблиці вживають однакові рядки статусів, тож достатньо
        /// звірити з одним набором — але саме з константою, не з літералом.
        /// </summary>
        private static string AnswerWord(string status) =>
            status == MembershipRequestStatus.Accepted ? "прийняв(ла)" : "відхилив(ла)";

        private static string MembershipTitle(string kind, string teamName, string nickname, string status) =>
            kind switch
            {
                NotificationKinds.MembershipInviteReceived =>
                    $"«{teamName}» запрошує вас до складу",
                NotificationKinds.MembershipApplicationReceived =>
                    $"{nickname} проситься до «{teamName}»",
                NotificationKinds.MembershipInviteAnswered =>
                    $"{nickname} {AnswerWord(status)} запрошення до «{teamName}»",
                _ =>
                    $"«{teamName}» {AnswerWord(status)} вашу заявку"
            };

        private static string TournamentTitle(string kind, string tournamentName, string teamName, string status) =>
            kind switch
            {
                NotificationKinds.TournamentInviteReceived =>
                    $"«{teamName}» запрошено на турнір «{tournamentName}»",
                NotificationKinds.TournamentApplicationReceived =>
                    $"«{teamName}» подала заявку на «{tournamentName}»",
                NotificationKinds.TournamentInviteAnswered =>
                    $"«{teamName}» {AnswerWord(status)} запрошення на «{tournamentName}»",
                _ =>
                    $"Організатор «{tournamentName}» {AnswerWord(status)} заявку «{teamName}»"
            };
    }
}
