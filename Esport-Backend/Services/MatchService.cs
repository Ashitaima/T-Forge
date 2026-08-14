using AutoMapper;
using TForge.Data.Interfaces;
using TForge.DTOs;
using TForge.Models;
using TForge.Services.Interfaces;
using TForge.Common;
using TForge.Common.Filters;
using TForge.Extensions;
using TForge.Exceptions;

namespace TForge.Services
{
    public class MatchService : IMatchService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBracketService _bracketService;
        private readonly IMapper _mapper;

        public MatchService(IUnitOfWork unitOfWork, IBracketService bracketService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _bracketService = bracketService;
            _mapper = mapper;
        }

        public async Task<IEnumerable<MatchDto>> GetAllAsync()
        {
            var matches = await _unitOfWork.Matches.GetAllAsync();
            return _mapper.Map<IEnumerable<MatchDto>>(matches);
        }

        public async Task<PagedResponse<MatchDto>> GetPagedAsync(PagedRequest request, MatchFilter? filter = null)
        {
            var query = _unitOfWork.Matches.GetQueryableWithDetails();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Status))
                {
                    query = query.Where(m => m.Status == filter.Status);
                }

                if (filter.TournamentId.HasValue)
                {
                    query = query.Where(m => m.TournamentId == filter.TournamentId.Value);
                }

                if (filter.TeamId.HasValue)
                {
                    query = query.Where(m => m.HomeTeamId == filter.TeamId.Value || m.AwayTeamId == filter.TeamId.Value);
                }

                if (filter.ScheduledFrom.HasValue)
                {
                    query = query.Where(m => m.ScheduledAt >= filter.ScheduledFrom.Value);
                }

                if (filter.ScheduledTo.HasValue)
                {
                    query = query.Where(m => m.ScheduledAt <= filter.ScheduledTo.Value);
                }

                if (!string.IsNullOrEmpty(filter.MatchType))
                {
                    query = query.Where(m => m.MatchType == filter.MatchType);
                }
            }

            if (!string.IsNullOrEmpty(request.Search))
            {
                query = query.ApplySearch(request.Search, "MatchType", "Format");
            }

            if (!string.IsNullOrEmpty(request.SortBy))
            {
                query = query.ApplySorting(request.SortBy, request.SortDirection);
            }

            return await query.ToPagedResponseAsync<Match, MatchDto>(request, _mapper);
        }

        public async Task<MatchDto?> GetByIdAsync(int id)
        {
            var match = await _unitOfWork.Matches.GetWithDetailsAsync(id);
            return match != null ? _mapper.Map<MatchDto>(match) : null;
        }

        public async Task<MatchDto?> GetWithDetailsAsync(int id)
        {
            var match = await _unitOfWork.Matches.GetWithDetailsAsync(id);
            return match != null ? _mapper.Map<MatchDto>(match) : null;
        }

        public async Task<IEnumerable<MatchDto>> GetByTournamentAsync(int tournamentId)
        {
            var matches = await _unitOfWork.Matches.GetByTournamentAsync(tournamentId);
            return _mapper.Map<IEnumerable<MatchDto>>(matches);
        }

        public async Task<IEnumerable<MatchDto>> GetByTeamAsync(int teamId)
        {
            var matches = await _unitOfWork.Matches.GetByTeamAsync(teamId);
            return _mapper.Map<IEnumerable<MatchDto>>(matches);
        }

        public async Task<IEnumerable<MatchDto>> GetByPlayerAsync(int playerId)
        {
            var matches = await _unitOfWork.Matches.GetAllAsync();
            var playerMatches = matches.Where(m => m.MatchPlayers.Any(mp => mp.PlayerId == playerId));
            return _mapper.Map<IEnumerable<MatchDto>>(playerMatches);
        }

        public async Task<IEnumerable<MatchDto>> GetByStatusAsync(string status)
        {
            var matches = await _unitOfWork.Matches.GetByStatusAsync(status);
            return _mapper.Map<IEnumerable<MatchDto>>(matches);
        }

        public async Task<IEnumerable<MatchDto>> GetScheduledMatchesAsync()
        {
            var matches = await _unitOfWork.Matches.GetByStatusAsync(MatchStatus.Scheduled);
            return _mapper.Map<IEnumerable<MatchDto>>(matches);
        }

        public async Task<IEnumerable<MatchDto>> GetCompletedMatchesAsync()
        {
            var matches = await _unitOfWork.Matches.GetByStatusAsync(MatchStatus.Completed);
            return _mapper.Map<IEnumerable<MatchDto>>(matches);
        }

        public async Task<MatchDto> CreateAsync(CreateMatchDto createDto)
        {
            var match = _mapper.Map<Match>(createDto);
            match.CreatedAt = DateTime.UtcNow;
            match.Status = MatchStatus.Scheduled;

            await _unitOfWork.Matches.AddAsync(match);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<MatchDto>(match);
        }

        public async Task<MatchDto?> UpdateAsync(int id, UpdateMatchDto updateDto)
        {
            var match = await _unitOfWork.Matches.GetByIdAsync(id);
            if (match == null)
                return null;

            _mapper.Map(updateDto, match);

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<MatchDto>(match);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var match = await _unitOfWork.Matches.GetByIdAsync(id);
            if (match == null)
                return false;

            _unitOfWork.Matches.Remove(match);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> StartMatchAsync(int id)
        {
            var match = await _unitOfWork.Matches.GetByIdAsync(id);
            if (match == null || match.Status != MatchStatus.Scheduled)
                return false;

            match.Status = MatchStatus.InProgress;
            match.StartedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CompleteMatchAsync(int id, int? winnerTeamId, string? result)
        {
            var match = await _unitOfWork.Matches.GetByIdAsync(id);
            if (match == null || match.Status == MatchStatus.Completed)
                return false;

            // Матч сітки не може завершитися внічию — переможець просувається далі
            if (match.Round > 0 && winnerTeamId == null)
            {
                throw new BusinessLogicException(
                    "Для матчу турнірної сітки потрібно вказати команду-переможця");
            }

            if (winnerTeamId.HasValue &&
                winnerTeamId.Value != match.HomeTeamId &&
                winnerTeamId.Value != match.AwayTeamId)
            {
                throw new BusinessLogicException(
                    "Переможцем може бути лише одна з команд, що грали цей матч");
            }

            match.Status = MatchStatus.Completed;
            match.WinnerTeamId = winnerTeamId;
            match.Notes = result ?? match.Notes;
            match.EndedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            await _bracketService.AdvanceAsync(match);

            return true;
        }

        public async Task<bool> CancelMatchAsync(int id, string? reason)
        {
            var match = await _unitOfWork.Matches.GetByIdAsync(id);
            if (match == null || match.Status == MatchStatus.Completed)
                return false;

            match.Status = MatchStatus.Cancelled;
            match.Notes = reason ?? match.Notes;

            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
