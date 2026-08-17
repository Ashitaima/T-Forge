using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TForge.Common;
using TForge.Data.Interfaces;
using TForge.DTOs;
using TForge.Exceptions;
using TForge.Models;
using TForge.Services.Interfaces;

namespace TForge.Services
{
    /// <summary>
    /// Запрошення й заявки на участь у турнірі. Дані читає цей сервіс, а кожне
    /// рішення про право на дію ухвалює чистий TournamentInvitationPolicy —
    /// так само, як для запитів на членство й викликів на матч.
    /// </summary>
    public class TournamentInvitationService : ITournamentInvitationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITournamentService _tournamentService;
        private readonly IMapper _mapper;

        public TournamentInvitationService(
            IUnitOfWork unitOfWork,
            ITournamentService tournamentService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _tournamentService = tournamentService;
            _mapper = mapper;
        }

        public Task<TournamentInvitationDto> InviteAsync(
            int tournamentId, int teamId, string message, int requestingUserId, bool isAdmin) =>
            CreateAsync(tournamentId, teamId, TournamentInvitationDirection.Invite,
                message, requestingUserId, isAdmin);

        public Task<TournamentInvitationDto> ApplyAsync(
            int tournamentId, int teamId, string message, int requestingUserId, bool isAdmin) =>
            CreateAsync(tournamentId, teamId, TournamentInvitationDirection.Application,
                message, requestingUserId, isAdmin);

        private async Task<TournamentInvitationDto> CreateAsync(
            int tournamentId, int teamId, string direction, string message, int requestingUserId, bool isAdmin)
        {
            var tournament = await _unitOfWork.Tournaments.GetWithTeamsAsync(tournamentId)
                ?? throw new EntityNotFoundException("Tournament", tournamentId);

            var team = await _unitOfWork.Teams.GetByIdAsync(teamId)
                ?? throw new EntityNotFoundException("Team", teamId);

            // Запрошення надсилає організатор, заявку — капітан команди.
            var allowed = direction == TournamentInvitationDirection.Invite
                ? TournamentInvitationPolicy.CanInvite(requestingUserId, tournament.OrganizerId, isAdmin)
                : TournamentInvitationPolicy.CanApply(requestingUserId, team.CaptainId, isAdmin);

            if (!allowed)
            {
                throw new ForbiddenException(direction == TournamentInvitationDirection.Invite
                    ? "Запросити команду може лише організатор турніру"
                    : "Подати заявку може лише капітан команди");
            }

            if (!tournament.IsActive)
            {
                throw new BusinessLogicException("Турнір неактивний");
            }

            if (tournament.Status != TournamentStatus.Registration)
            {
                throw new BusinessLogicException("Реєстрацію на цей турнір закрито");
            }

            if (!team.IsActive)
            {
                throw new BusinessLogicException("Команда неактивна");
            }

            if (tournament.Teams.Any(t => t.Id == teamId))
            {
                throw new BusinessLogicException("Команду вже зареєстровано на цей турнір");
            }

            var hasPending = await _unitOfWork.TournamentInvitations.ExistsAsync(i =>
                i.TournamentId == tournamentId
                && i.TeamId == teamId
                && i.Status == TournamentInvitationStatus.Pending);

            if (hasPending)
            {
                throw new BusinessLogicException("Запит для цієї пари вже очікує на відповідь");
            }

            var invitation = new TournamentInvitation
            {
                TournamentId = tournamentId,
                TeamId = teamId,
                Direction = direction,
                Status = TournamentInvitationStatus.Pending,
                InitiatedByUserId = requestingUserId,
                Message = message ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.TournamentInvitations.AddAsync(invitation);
            await _unitOfWork.SaveChangesAsync();

            return await LoadDtoAsync(invitation.Id);
        }

        public async Task<TournamentInvitationDto> AcceptAsync(
            int invitationId, int requestingUserId, bool isAdmin)
        {
            var invitation = await GetWithRelationsAsync(invitationId);

            if (!TournamentInvitationPolicy.CanRespond(ToPolicyContext(invitation), requestingUserId, isAdmin))
            {
                throw MakeRespondError(invitation);
            }

            // Реєстрація перевіряє стан турніру й може відмовити (місць немає,
            // реєстрацію закрито). Тоді запит лишається відкритим — прийняти
            // його можна буде, якщо місце звільниться.
            await _tournamentService.AdmitTeamAsync(invitation.TournamentId, invitation.TeamId);

            invitation.Status = TournamentInvitationStatus.Accepted;
            invitation.RespondedAt = DateTime.UtcNow;
            invitation.RespondedByUserId = requestingUserId;

            await _unitOfWork.SaveChangesAsync();

            return await LoadDtoAsync(invitation.Id);
        }

        public async Task<TournamentInvitationDto> DeclineAsync(
            int invitationId, int requestingUserId, bool isAdmin)
        {
            var invitation = await GetWithRelationsAsync(invitationId);

            if (!TournamentInvitationPolicy.CanRespond(ToPolicyContext(invitation), requestingUserId, isAdmin))
            {
                throw MakeRespondError(invitation);
            }

            invitation.Status = TournamentInvitationStatus.Declined;
            invitation.RespondedAt = DateTime.UtcNow;
            invitation.RespondedByUserId = requestingUserId;

            await _unitOfWork.SaveChangesAsync();

            return await LoadDtoAsync(invitation.Id);
        }

        public async Task<TournamentInvitationDto> CancelAsync(
            int invitationId, int requestingUserId, bool isAdmin)
        {
            var invitation = await GetWithRelationsAsync(invitationId);

            if (!TournamentInvitationPolicy.CanCancel(ToPolicyContext(invitation), requestingUserId, isAdmin))
            {
                if (!TournamentInvitationPolicy.IsPending(ToPolicyContext(invitation)))
                {
                    throw new BusinessLogicException("Запит уже закрито");
                }

                throw new ForbiddenException("Скасувати запит може лише той, хто його створив");
            }

            invitation.Status = TournamentInvitationStatus.Cancelled;
            invitation.RespondedAt = DateTime.UtcNow;
            invitation.RespondedByUserId = requestingUserId;

            await _unitOfWork.SaveChangesAsync();

            return await LoadDtoAsync(invitation.Id);
        }

        public Task<IEnumerable<TournamentInvitationDto>> GetForTournamentAsync(
            int tournamentId, string? status) =>
            QueryAsync(i => i.TournamentId == tournamentId, status);

        public Task<IEnumerable<TournamentInvitationDto>> GetForTeamAsync(int teamId, string? status) =>
            QueryAsync(i => i.TeamId == teamId, status);

        private async Task<IEnumerable<TournamentInvitationDto>> QueryAsync(
            System.Linq.Expressions.Expression<Func<TournamentInvitation, bool>> predicate,
            string? status)
        {
            var query = BaseQuery().Where(predicate);

            if (!string.IsNullOrEmpty(status))
            {
                if (!TournamentInvitationStatus.IsValid(status))
                {
                    throw new BusinessLogicException($"Невідомий статус запиту: {status}");
                }

                query = query.Where(i => i.Status == status);
            }

            var rows = await query
                .OrderByDescending(i => i.CreatedAt)
                .ThenByDescending(i => i.Id)
                .ToListAsync();

            return _mapper.Map<IEnumerable<TournamentInvitationDto>>(rows);
        }

        private IQueryable<TournamentInvitation> BaseQuery() =>
            _unitOfWork.TournamentInvitations.GetQueryable()
                .Include(i => i.Tournament)
                .Include(i => i.Team);

        private async Task<TournamentInvitation> GetWithRelationsAsync(int invitationId) =>
            await BaseQuery().FirstOrDefaultAsync(i => i.Id == invitationId)
                ?? throw new EntityNotFoundException("TournamentInvitation", invitationId);

        private async Task<TournamentInvitationDto> LoadDtoAsync(int invitationId) =>
            _mapper.Map<TournamentInvitationDto>(await GetWithRelationsAsync(invitationId));

        private static TournamentInvitationPolicy.Context ToPolicyContext(TournamentInvitation invitation) =>
            new(invitation.Direction,
                invitation.Status,
                invitation.InitiatedByUserId,
                invitation.Tournament.OrganizerId,
                invitation.Team.CaptainId);

        /// <summary>
        /// Розрізняє «запит уже закрито» (400) і «ви не та сторона» (403),
        /// бо CanRespond повертає false в обох випадках.
        /// </summary>
        private static Exception MakeRespondError(TournamentInvitation invitation)
        {
            if (!TournamentInvitationPolicy.IsPending(ToPolicyContext(invitation)))
            {
                return new BusinessLogicException("Запит уже закрито");
            }

            return new ForbiddenException(invitation.Direction == TournamentInvitationDirection.Invite
                ? "Відповісти на запрошення може лише капітан запрошеної команди"
                : "Відповісти на заявку може лише організатор турніру");
        }
    }
}
