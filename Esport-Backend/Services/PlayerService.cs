using AutoMapper;
using TForge.Data.Interfaces;
using TForge.DTOs;
using TForge.Models;
using TForge.Services.Interfaces;
using TForge.Common;
using TForge.Common.Filters;
using TForge.Extensions;
using Microsoft.EntityFrameworkCore;

namespace TForge.Services
{
    public class PlayerService : IPlayerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PlayerService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PlayerDto>> GetAllAsync()
        {
            var players = await _unitOfWork.Players.GetAllAsync();
            return _mapper.Map<IEnumerable<PlayerDto>>(players);
        }

        public async Task<PagedResponse<PlayerDto>> GetPagedAsync(PagedRequest request, PlayerFilter? filter = null)
        {
            IQueryable<Player> query = _unitOfWork.Players.GetQueryable()
                .Include(p => p.Team)
                .Include(p => p.User);

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Position))
                {
                    query = query.Where(p => p.Position == filter.Position);
                }

                if (!string.IsNullOrEmpty(filter.Country))
                {
                    query = query.Where(p => p.Country == filter.Country);
                }

                if (filter.MinAge.HasValue)
                {
                    query = query.Where(p => p.Age >= filter.MinAge.Value);
                }

                if (filter.MaxAge.HasValue)
                {
                    query = query.Where(p => p.Age <= filter.MaxAge.Value);
                }

                if (filter.TeamId.HasValue)
                {
                    query = query.Where(p => p.TeamId == filter.TeamId.Value);
                }

                if (filter.IsActive.HasValue)
                {
                    query = query.Where(p => p.IsActive == filter.IsActive.Value);
                }

                if (filter.FreeAgents.HasValue && filter.FreeAgents.Value)
                {
                    query = query.Where(p => p.TeamId == null);
                }
            }

            if (!string.IsNullOrEmpty(request.Search))
            {
                query = query.ApplySearch(request.Search, "Nickname");
            }

            if (!string.IsNullOrEmpty(request.SortBy))
            {
                query = query.ApplySorting(request.SortBy, request.SortDirection);
            }

            return await query.ToPagedResponseAsync<Player, PlayerDto>(request, _mapper);
        }

        public async Task<PlayerDto?> GetByIdAsync(int id)
        {
            var player = await _unitOfWork.Players.GetByIdAsync(id);
            return player != null ? _mapper.Map<PlayerDto>(player) : null;
        }

        public async Task<PlayerDto?> GetWithTeamAsync(int id)
        {
            var player = await _unitOfWork.Players.GetByIdAsync(id);
            return player != null ? _mapper.Map<PlayerDto>(player) : null;
        }

        public async Task<PlayerDto?> GetWithMatchesAsync(int id)
        {
            var player = await _unitOfWork.Players.GetByIdAsync(id);
            return player != null ? _mapper.Map<PlayerDto>(player) : null;
        }

        public async Task<IEnumerable<PlayerDto>> GetByTeamAsync(int teamId)
        {
            var players = await _unitOfWork.Players.GetByTeamAsync(teamId);
            return _mapper.Map<IEnumerable<PlayerDto>>(players);
        }

        public async Task<IEnumerable<PlayerDto>> GetFreeAgentsAsync()
        {
            var players = await _unitOfWork.Players.GetAllAsync();
            var freeAgents = players.Where(p => !p.TeamId.HasValue && p.IsActive);
            return _mapper.Map<IEnumerable<PlayerDto>>(freeAgents);
        }

        public async Task<PlayerDto> CreateAsync(CreatePlayerDto createDto, int userId)
        {
            var player = _mapper.Map<Player>(createDto);
            player.UserId = userId;

            await _unitOfWork.Players.AddAsync(player);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PlayerDto>(player);
        }

        public async Task<PlayerDto?> UpdateAsync(int id, UpdatePlayerDto updateDto)
        {
            var player = await _unitOfWork.Players.GetByIdAsync(id);
            if (player == null)
                return null;

            _mapper.Map(updateDto, player);

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PlayerDto>(player);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var player = await _unitOfWork.Players.GetByIdAsync(id);
            if (player == null)
                return false;

            _unitOfWork.Players.Remove(player);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> JoinTeamAsync(int playerId, int teamId)
        {
            var player = await _unitOfWork.Players.GetByIdAsync(playerId);
            var team = await _unitOfWork.Teams.GetByIdAsync(teamId);

            if (player == null || team == null)
                return false;

            if (player.TeamId.HasValue)
                return false;

            player.TeamId = teamId;

            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> LeaveTeamAsync(int playerId)
        {
            var player = await _unitOfWork.Players.GetByIdAsync(playerId);

            if (player == null || !player.TeamId.HasValue)
                return false;

            player.TeamId = null;

            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
