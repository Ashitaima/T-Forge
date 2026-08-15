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

        public Task<MembershipRequestDto> AcceptAsync(int requestId, int requestingUserId, bool isAdmin) =>
            throw new NotImplementedException();

        public Task<MembershipRequestDto> DeclineAsync(int requestId, int requestingUserId, bool isAdmin) =>
            throw new NotImplementedException();

        public Task<MembershipRequestDto> CancelAsync(int requestId, int requestingUserId, bool isAdmin) =>
            throw new NotImplementedException();

        public Task<IEnumerable<MembershipRequestDto>> GetForTeamAsync(int teamId, string? status, int requestingUserId, bool isAdmin) =>
            throw new NotImplementedException();

        public Task<IEnumerable<MembershipRequestDto>> GetForPlayerAsync(int playerId, string? status, int requestingUserId, bool isAdmin) =>
            throw new NotImplementedException();
    }
}
