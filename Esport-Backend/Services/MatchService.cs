using Microsoft.EntityFrameworkCore;
using AutoMapper;
using TForge.Data.Interfaces;
using TForge.DTOs;
using TForge.Models;
using TForge.Services.Interfaces;
using TForge.Common;
using TForge.Common.Filters;
using TForge.Extensions;
using TForge.Exceptions;
using TForge.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace TForge.Services
{
    public class MatchService : IMatchService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBracketService _bracketService;
        private readonly IMatchRosterService _rosterService;
        private readonly IHubContext<MatchHub> _hub;
        private readonly IMapper _mapper;

        public MatchService(
            IUnitOfWork unitOfWork,
            IBracketService bracketService,
            IMatchRosterService rosterService,
            IHubContext<MatchHub> hub,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _bracketService = bracketService;
            _rosterService = rosterService;
            _hub = hub;
            _mapper = mapper;
        }

        /// <summary>Оновлює рахунок матчу, що триває, і розсилає його підписникам.</summary>
        public async Task<MatchDto> UpdateScoreAsync(int id, UpdateScoreDto dto)
        {
            var match = await _unitOfWork.Matches.GetByIdAsync(id)
                ?? throw new EntityNotFoundException("Match", id);

            if (match.Status != MatchStatus.InProgress)
            {
                throw new BusinessLogicException("Рахунок можна змінювати лише під час матчу");
            }

            if (dto.HomeTeamScore < 0 || dto.AwayTeamScore < 0)
            {
                throw new BusinessLogicException("Рахунок не може бути відʼємним");
            }

            match.HomeTeamScore = dto.HomeTeamScore;
            match.AwayTeamScore = dto.AwayTeamScore;
            await _unitOfWork.SaveChangesAsync();

            await _hub.Clients.Group(MatchHub.GroupFor(id)).SendAsync(
                MatchHubEvents.ScoreUpdated,
                new { matchId = id, homeTeamScore = match.HomeTeamScore, awayTeamScore = match.AwayTeamScore });

            return _mapper.Map<MatchDto>(match);
        }

        private Task BroadcastStatusAsync(Match match) =>
            _hub.Clients.Group(MatchHub.GroupFor(match.Id)).SendAsync(
                MatchHubEvents.MatchStatusChanged,
                new
                {
                    matchId = match.Id,
                    status = match.Status,
                    homeTeamScore = match.HomeTeamScore,
                    awayTeamScore = match.AwayTeamScore,
                    winnerTeamId = match.WinnerTeamId
                });

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

                if (!string.IsNullOrEmpty(filter.Game))
                {
                    query = query.Where(m => m.Game == filter.Game);
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
            var tournament = await _unitOfWork.Tournaments.GetByIdAsync(createDto.TournamentId)
                ?? throw new EntityNotFoundException("Tournament", createDto.TournamentId);

            var match = _mapper.Map<Match>(createDto);
            match.CreatedAt = DateTime.UtcNow;
            match.Status = MatchStatus.Scheduled;
            // Дисципліна успадковується від турніру, а не приходить від клієнта.
            match.Game = tournament.Game;

            await _unitOfWork.Matches.AddAsync(match);
            await _unitOfWork.SaveChangesAsync();

            // Склад проставляється одразу: ростер матчу — це нормальний
            // стартовий стан, а не те, що організатор має згадати заповнити.
            // Кнопка «Заповнити зі складів» лишається, щоб підтягнути новачків
            // після трансферу — повторний виклик уже доданих не чіпає.
            await _rosterService.AutoFillAsync(match.Id);

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

        /// <summary>
        /// Читає лише те, що потрібно політиці: чи це турнірний матч і хто
        /// капітани обох команд.
        /// </summary>
        public async Task<FriendlyMatchPolicy.Context> GetManageContextAsync(int id)
        {
            var context = await _unitOfWork.Matches.GetQueryable()
                .Where(m => m.Id == id)
                .Select(m => new FriendlyMatchPolicy.Context(
                    m.TournamentId,
                    m.HomeTeam.CaptainId,
                    m.AwayTeam.CaptainId))
                .FirstOrDefaultAsync();

            return context ?? throw new EntityNotFoundException("Match", id);
        }

        public async Task<MatchDto> UpdateLinksAsync(int id, string? streamUrl, string? trackerUrl)
        {
            var match = await _unitOfWork.Matches.GetByIdAsync(id)
                ?? throw new EntityNotFoundException("Match", id);

            match.StreamUrl = NormalizeStreamUrl(streamUrl);
            match.TrackerUrl = NormalizeTrackerUrl(trackerUrl);

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<MatchDto>(match);
        }

        /// <summary>Порожнє поле означає «трансляції немає».</summary>
        private static string? NormalizeStreamUrl(string? streamUrl)
        {
            var trimmed = streamUrl?.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                return null;
            }

            if (!StreamUrlRules.IsValid(trimmed))
            {
                throw new BusinessLogicException(
                    "Посилання має вести на Twitch або YouTube і починатися з https://");
            }

            return trimmed;
        }

        /// <summary>Порожнє поле означає «трекера немає».</summary>
        private static string? NormalizeTrackerUrl(string? trackerUrl)
        {
            var trimmed = trackerUrl?.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                return null;
            }

            if (!TrackerUrlRules.IsValid(trimmed))
            {
                throw new BusinessLogicException(
                    "Посилання на трекер має починатися з https:// і бути не довшим за 300 символів");
            }

            return trimmed;
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
            await BroadcastStatusAsync(match);

            return true;
        }

        public async Task<bool> CompleteMatchAsync(int id, int? winnerTeamId, string? result, string? trackerUrl)
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

            // Посилання на трекер зазвичай зʼявляється саме в момент завершення.
            // Порожнє поле лишає наявне значення, а не стирає його.
            if (!string.IsNullOrWhiteSpace(trackerUrl))
            {
                match.TrackerUrl = NormalizeTrackerUrl(trackerUrl);
            }

            await _unitOfWork.SaveChangesAsync();

            await _rosterService.ApplyMatchResultAsync(match);
            await _bracketService.AdvanceAsync(match);
            await BroadcastStatusAsync(match);

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
            await BroadcastStatusAsync(match);

            return true;
        }
    }
}
