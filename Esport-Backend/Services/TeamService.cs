using AutoMapper;
using Computational_Practice.Data.Interfaces;
using Computational_Practice.DTOs;
using Computational_Practice.Models;
using Computational_Practice.Services.Interfaces;
using Computational_Practice.Common;
using Computational_Practice.Common.Filters;
using Computational_Practice.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Computational_Practice.Services
{
    public class TeamService : ITeamService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public TeamService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TeamDto>> GetAllAsync()
        {
            var teams = await _unitOfWork.Teams.GetAllAsync();
            return _mapper.Map<IEnumerable<TeamDto>>(teams);
        }

        public async Task<PagedResponse<TeamDto>> GetPagedAsync(PagedRequest request, TeamFilter? filter = null)
        {
            IQueryable<Team> query = _unitOfWork.Teams.GetQueryable().Include(t => t.Captain);

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Name))
                {
                    query = query.Where(t => t.Name.Contains(filter.Name));
                }

                if (filter.TournamentId.HasValue)
                {
                    query = query.Where(t => t.Tournaments.Any(tour => tour.Id == filter.TournamentId.Value));
                }

                if (filter.MinPlayersCount.HasValue)
                {
                    query = query.Where(t => t.Players.Count >= filter.MinPlayersCount.Value);
                }

                if (filter.MaxPlayersCount.HasValue)
                {
                    query = query.Where(t => t.Players.Count <= filter.MaxPlayersCount.Value);
                }
            }

            if (!string.IsNullOrEmpty(request.Search))
            {
                query = query.ApplySearch(request.Search, "Name");
            }

            if (!string.IsNullOrEmpty(request.SortBy))
            {
                query = query.ApplySorting(request.SortBy, request.SortDirection);
            }

            return await query.ToPagedResponseAsync<Team, TeamDto>(request, _mapper);
        }

        public async Task<TeamDto?> GetByIdAsync(int id)
        {
            var team = await _unitOfWork.Teams.GetByIdAsync(id);
            return team != null ? _mapper.Map<TeamDto>(team) : null;
        }

        public async Task<TeamDto?> GetWithPlayersAsync(int id)
        {
            var team = await _unitOfWork.Teams.GetWithPlayersAsync(id);
            return team != null ? _mapper.Map<TeamDto>(team) : null;
        }

        public async Task<IEnumerable<TeamDto>> GetByTournamentAsync(int tournamentId)
        {
            var teams = await _unitOfWork.Teams.GetAllAsync();
            var tournamentTeams = teams.Where(t => t.Tournaments.Any(tour => tour.Id == tournamentId));
            return _mapper.Map<IEnumerable<TeamDto>>(tournamentTeams);
        }

        public async Task<TeamDto> CreateAsync(CreateTeamDto createDto)
        {
            var team = _mapper.Map<Team>(createDto);
            team.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.Teams.AddAsync(team);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<TeamDto>(team);
        }

        public async Task<TeamDto?> UpdateAsync(int id, UpdateTeamDto updateDto)
        {
            var team = await _unitOfWork.Teams.GetByIdAsync(id);
            if (team == null)
                return null;

            _mapper.Map(updateDto, team);

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<TeamDto>(team);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var team = await _unitOfWork.Teams.GetByIdAsync(id);
            if (team == null)
                return false;

            _unitOfWork.Teams.Remove(team);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> AddPlayerToTeamAsync(int teamId, int playerId)
        {
            var team = await _unitOfWork.Teams.GetByIdAsync(teamId);
            var player = await _unitOfWork.Players.GetByIdAsync(playerId);

            if (team == null || player == null)
                return false;

            if (player.TeamId.HasValue)
                return false;

            player.TeamId = teamId;

            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemovePlayerFromTeamAsync(int teamId, int playerId)
        {
            var player = await _unitOfWork.Players.GetByIdAsync(playerId);

            if (player == null || player.TeamId != teamId)
                return false;

            player.TeamId = null;

            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
