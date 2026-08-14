using AutoMapper;
using TForge.Data.Interfaces;
using TForge.DTOs;
using TForge.Models;
using TForge.Services.Interfaces;
using TForge.Common;
using TForge.Common.Filters;
using TForge.Extensions;
using Microsoft.EntityFrameworkCore;
using TForge.Exceptions;

namespace TForge.Services
{
    public class PlayerService : IPlayerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStandingsService _standingsService;
        private readonly IMapper _mapper;

        public PlayerService(IUnitOfWork unitOfWork, IStandingsService standingsService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _standingsService = standingsService;
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

        public async Task<PlayerProfileDto> GetProfileAsync(int id)
        {
            var player = await _unitOfWork.Players.GetQueryable()
                .Include(p => p.User)
                .Include(p => p.Team)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new EntityNotFoundException("Player", id);

            var career = await _standingsService.GetPlayerCareerAsync(id);

            return new PlayerProfileDto
            {
                Player = _mapper.Map<PlayerDto>(player),
                Matches = career.Matches,
                Wins = career.Wins,
                Losses = career.Losses,
                WinRate = career.WinRate,
                Kills = career.Kills,
                Deaths = career.Deaths,
                Assists = career.Assists,
                Kda = career.Kda
            };
        }

        public async Task<PlayerDto?> GetWithTeamAsync(int id)
        {
            var player = await _unitOfWork.Players.GetByIdAsync(id);
            return player != null ? _mapper.Map<PlayerDto>(player) : null;
        }

        public async Task<PagedResponse<PlayerMatchDto>> GetMatchLogAsync(int id, PagedRequest request)
        {
            var exists = await _unitOfWork.Players.ExistsAsync(p => p.Id == id);
            if (!exists)
            {
                throw new EntityNotFoundException("Player", id);
            }

            var query = _unitOfWork.MatchPlayers.GetQueryable()
                .Include(mp => mp.Team)
                .Include(mp => mp.Match).ThenInclude(m => m.HomeTeam)
                .Include(mp => mp.Match).ThenInclude(m => m.AwayTeam)
                .Include(mp => mp.Match).ThenInclude(m => m.Tournament)
                .Where(mp => mp.PlayerId == id)
                .OrderByDescending(mp => mp.Match.ScheduledAt)
                .ThenByDescending(mp => mp.MatchId);

            var totalCount = await query.CountAsync();
            var rows = await query.Skip(request.Skip).Take(request.Take).ToListAsync();

            return new PagedResponse<PlayerMatchDto>
            {
                Data = rows.Select(ToLogEntry).ToList(),
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        /// <summary>
        /// Суперник — це та зі сторін матчу, якою не є команда з рядка ростера,
        /// тож журнал лишається правильним навіть після трансферу гравця.
        /// </summary>
        private PlayerMatchDto ToLogEntry(MatchPlayer entry)
        {
            var match = entry.Match;
            var playedAtHome = entry.TeamId == match.HomeTeamId;

            var opponent = playedAtHome ? match.AwayTeam : match.HomeTeam;
            var teamScore = playedAtHome ? match.HomeTeamScore : match.AwayTeamScore;
            var opponentScore = playedAtHome ? match.AwayTeamScore : match.HomeTeamScore;

            var result = match.Status != MatchStatus.Completed || match.WinnerTeamId == null
                ? ResultType.Pending
                : match.WinnerTeamId == entry.TeamId ? ResultType.Win : ResultType.Loss;

            return new PlayerMatchDto
            {
                MatchId = match.Id,
                ScheduledAt = match.ScheduledAt,
                Status = match.Status,
                PlayedFor = _mapper.Map<TeamSummaryDto>(entry.Team),
                Opponent = opponent == null ? null : _mapper.Map<TeamSummaryDto>(opponent),
                TeamScore = teamScore,
                OpponentScore = opponentScore,
                Result = result,
                TournamentName = match.Tournament?.Name,
                MatchType = match.MatchType,
                Kills = entry.Kills,
                Deaths = entry.Deaths,
                Assists = entry.Assists,
                Champion = entry.Champion
            };
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
