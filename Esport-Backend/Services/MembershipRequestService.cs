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
    /// Запити на членство в команді. Дані читає цей сервіс, а кожне рішення
    /// про право на дію ухвалює чистий MembershipRequestPolicy.
    /// </summary>
    public class MembershipRequestService : IMembershipRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public MembershipRequestService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public Task<MembershipRequestDto> InviteAsync(int teamId, int playerId, int requestingUserId, bool isAdmin) =>
            CreateAsync(teamId, playerId, MembershipRequestDirection.Invite, requestingUserId, isAdmin);

        public Task<MembershipRequestDto> ApplyAsync(int playerId, int teamId, int requestingUserId, bool isAdmin) =>
            CreateAsync(teamId, playerId, MembershipRequestDirection.Application, requestingUserId, isAdmin);

        private async Task<MembershipRequestDto> CreateAsync(
            int teamId, int playerId, string direction, int requestingUserId, bool isAdmin)
        {
            var team = await _unitOfWork.Teams.GetByIdAsync(teamId)
                ?? throw new EntityNotFoundException("Team", teamId);

            var player = await _unitOfWork.Players.GetByIdAsync(playerId)
                ?? throw new EntityNotFoundException("Player", playerId);

            // Запрошення надсилає капітан, заявку — сам гравець.
            if (!isAdmin)
            {
                var allowed = direction == MembershipRequestDirection.Invite
                    ? team.CaptainId == requestingUserId
                    : player.UserId == requestingUserId;

                if (!allowed)
                {
                    throw new ForbiddenException(direction == MembershipRequestDirection.Invite
                        ? "Запросити гравця може лише капітан команди"
                        : "Подати заявку може лише сам гравець");
                }
            }

            if (!team.IsActive)
            {
                throw new BusinessLogicException("Команда неактивна");
            }

            if (player.TeamId == teamId)
            {
                throw new BusinessLogicException("Гравець уже входить до цієї команди");
            }

            var hasPending = await _unitOfWork.MembershipRequests.ExistsAsync(r =>
                r.TeamId == teamId
                && r.PlayerId == playerId
                && r.Status == MembershipRequestStatus.Pending);

            if (hasPending)
            {
                throw new BusinessLogicException(
                    "Запит для цієї пари вже очікує на відповідь");
            }

            var request = new TeamMembershipRequest
            {
                TeamId = teamId,
                PlayerId = playerId,
                Direction = direction,
                Status = MembershipRequestStatus.Pending,
                InitiatedByUserId = requestingUserId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.MembershipRequests.AddAsync(request);
            await _unitOfWork.SaveChangesAsync();

            return await LoadDtoAsync(request.Id);
        }

        /// <summary>Перечитує запит із командою та гравцем, щоб DTO був повним.</summary>
        private async Task<MembershipRequestDto> LoadDtoAsync(int requestId)
        {
            var loaded = await _unitOfWork.MembershipRequests.GetQueryable()
                .Include(r => r.Team)
                .Include(r => r.Player)
                .FirstOrDefaultAsync(r => r.Id == requestId)
                ?? throw new EntityNotFoundException("MembershipRequest", requestId);

            return _mapper.Map<MembershipRequestDto>(loaded);
        }

        /// <summary>Читає запит разом із командою та гравцем — потрібні id капітана й користувача.</summary>
        private async Task<TeamMembershipRequest> GetWithRelationsAsync(int requestId)
        {
            return await _unitOfWork.MembershipRequests.GetQueryable()
                .Include(r => r.Team)
                .Include(r => r.Player)
                .FirstOrDefaultAsync(r => r.Id == requestId)
                ?? throw new EntityNotFoundException("MembershipRequest", requestId);
        }

        private static MembershipRequestPolicy.Context ToPolicyContext(TeamMembershipRequest request) =>
            new(request.Direction,
                request.Status,
                request.InitiatedByUserId,
                request.Team.CaptainId,
                request.Player.UserId);

        public async Task<MembershipRequestDto> AcceptAsync(int requestId, int requestingUserId, bool isAdmin)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var request = await GetWithRelationsAsync(requestId);

                if (!MembershipRequestPolicy.CanRespond(ToPolicyContext(request), requestingUserId, isAdmin))
                {
                    throw MakeRespondError(request, requestingUserId, isAdmin);
                }

                // Умову перевіряє сама БД: оновлюємо лише той рядок, де команда ще порожня.
                // Транзакція під Read Committed не блокує рядок під час читання, тому
                // перевірка "до" і запис "після" — це різні миті, між якими інша транзакція
                // встигає прийняти свій запит. Умовний UPDATE закриває цей проміжок.
                var claimed = await _unitOfWork.Players.GetQueryable()
                    .Where(p => p.Id == request.PlayerId && p.TeamId == null)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.TeamId, request.TeamId));

                if (claimed == 0)
                {
                    throw new BusinessLogicException("Спочатку потрібно покинути поточну команду");
                }

                request.Status = MembershipRequestStatus.Accepted;
                request.RespondedAt = DateTime.UtcNow;
                request.RespondedByUserId = requestingUserId;

                // Інші відкриті запити цього гравця вже неможливо прийняти,
                // тож закриваємо їх, щоб капітани не бачили мертвих заявок.
                var otherPending = await _unitOfWork.MembershipRequests.FindAsync(r =>
                    r.PlayerId == request.PlayerId
                    && r.Id != request.Id
                    && r.Status == MembershipRequestStatus.Pending);

                foreach (var other in otherPending)
                {
                    other.Status = MembershipRequestStatus.Cancelled;
                    other.RespondedAt = DateTime.UtcNow;
                    other.RespondedByUserId = requestingUserId;
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return await LoadDtoAsync(request.Id);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<MembershipRequestDto> DeclineAsync(int requestId, int requestingUserId, bool isAdmin)
        {
            var request = await GetWithRelationsAsync(requestId);

            if (!MembershipRequestPolicy.CanRespond(ToPolicyContext(request), requestingUserId, isAdmin))
            {
                throw MakeRespondError(request, requestingUserId, isAdmin);
            }

            request.Status = MembershipRequestStatus.Declined;
            request.RespondedAt = DateTime.UtcNow;
            request.RespondedByUserId = requestingUserId;

            await _unitOfWork.SaveChangesAsync();

            return await LoadDtoAsync(request.Id);
        }

        public async Task<MembershipRequestDto> CancelAsync(int requestId, int requestingUserId, bool isAdmin)
        {
            var request = await GetWithRelationsAsync(requestId);

            if (!MembershipRequestPolicy.CanCancel(ToPolicyContext(request), requestingUserId, isAdmin))
            {
                if (!MembershipRequestPolicy.IsPending(ToPolicyContext(request)))
                {
                    throw new BusinessLogicException("Запит уже закрито");
                }

                throw new ForbiddenException("Скасувати запит може лише той, хто його створив");
            }

            request.Status = MembershipRequestStatus.Cancelled;
            request.RespondedAt = DateTime.UtcNow;
            request.RespondedByUserId = requestingUserId;

            await _unitOfWork.SaveChangesAsync();

            return await LoadDtoAsync(request.Id);
        }

        /// <summary>
        /// Розрізняє «запит уже закрито» (400) і «ви не та сторона» (403),
        /// бо CanRespond повертає false в обох випадках.
        /// </summary>
        private static Exception MakeRespondError(TeamMembershipRequest request, int requestingUserId, bool isAdmin)
        {
            if (!MembershipRequestPolicy.IsPending(ToPolicyContext(request)))
            {
                return new BusinessLogicException("Запит уже закрито");
            }

            return new ForbiddenException(request.Direction == MembershipRequestDirection.Invite
                ? "Відповісти на запрошення може лише запрошений гравець"
                : "Відповісти на заявку може лише капітан команди");
        }

        public Task<IEnumerable<MembershipRequestDto>> GetForTeamAsync(int teamId, string? status, int requestingUserId, bool isAdmin) =>
            throw new NotImplementedException();

        public Task<IEnumerable<MembershipRequestDto>> GetForPlayerAsync(int playerId, string? status, int requestingUserId, bool isAdmin) =>
            throw new NotImplementedException();
    }
}
