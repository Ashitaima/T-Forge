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

        /// <summary>
        /// Дані, потрібні MatchCreationPolicy, щоб вирішити, кому можна
        /// створити саме цей матч. Читаємо одним запитом ще до запису.
        /// </summary>
        /// <summary>
        /// Домашня команда матчу, який створює капітан. Клієнт її не надсилає:
        /// вона випливає з капітанства. Якщо капітан веде кілька команд —
        /// просимо уточнити, бо вгадувати за нього не можна.
        /// </summary>
        private async Task<int> ResolveHomeTeamIdAsync(CreateMatchDto createDto, int requestingUserId)
        {
            if (createDto.HomeTeamId is int explicitId)
            {
                return explicitId;
            }

            var captained = await _unitOfWork.Teams.GetQueryable()
                .Where(t => t.CaptainId == requestingUserId)
                .Select(t => t.Id)
                .ToListAsync();

            return captained.Count switch
            {
                1 => captained[0],
                0 => throw new BusinessLogicException(
                    "Створити матч може капітан команди — у вас її немає"),
                _ => throw new BusinessLogicException(
                    "Ви капітан кількох команд — оберіть, яка з них грає")
            };
        }

        public async Task<MatchCreationPolicy.Context> GetCreateContextAsync(
            CreateMatchDto createDto, int requestingUserId)
        {
            // Домашню команду треба вивести ще до перевірки прав: капітан її не
            // надсилає, і без цього контекст лишався б без жодного капітана —
            // тобто відкритий матч не міг би створити ніхто.
            var homeTeamId = await ResolveHomeTeamIdAsync(createDto, requestingUserId);

            var organizerUserId = createDto.TournamentId == null
                ? null
                : await _unitOfWork.Tournaments.GetQueryable()
                    .Where(t => t.Id == createDto.TournamentId)
                    .Select(t => (int?)t.OrganizerId)
                    .FirstOrDefaultAsync();

            var captains = await _unitOfWork.Teams.GetQueryable()
                .Where(t => t.Id == homeTeamId || t.Id == createDto.AwayTeamId)
                .Select(t => new { t.Id, t.CaptainId })
                .ToListAsync();

            return new MatchCreationPolicy.Context(
                createDto.TournamentId,
                organizerUserId,
                captains.FirstOrDefault(t => t.Id == homeTeamId)?.CaptainId,
                captains.FirstOrDefault(t => t.Id == createDto.AwayTeamId)?.CaptainId);
        }

        /// <summary>
        /// Приєднатися до відкритого матчу — це і є назватися гостем. Доти
        /// AwayTeamId порожній, і саме тут він уперше отримує значення.
        /// </summary>
        public async Task<MatchDto> JoinAsync(int id, int requestingUserId, bool isAdmin)
        {
            var match = await _unitOfWork.Matches.GetByIdAsync(id)
                ?? throw new EntityNotFoundException("Match", id);

            if (match.AwayTeamId != null)
            {
                throw new BusinessLogicException("До цього матчу вже приєдналися");
            }

            if (match.Status != MatchStatus.Scheduled)
            {
                throw new BusinessLogicException("Приєднатися можна лише до запланованого матчу");
            }

            var captained = await _unitOfWork.Teams.GetQueryable()
                .Where(t => t.CaptainId == requestingUserId && t.Id != match.HomeTeamId)
                .Select(t => t.Id)
                .ToListAsync();

            var awayTeamId = captained.Count switch
            {
                1 => captained[0],
                0 => throw new ForbiddenException(
                    "Приєднатися до матчу може лише капітан іншої команди"),
                _ => throw new BusinessLogicException(
                    "Ви капітан кількох команд — оберіть, яка з них грає")
            };

            match.AwayTeamId = awayTeamId;
            await _unitOfWork.SaveChangesAsync();

            // Склад гостя доливаємо тим самим шляхом, що й при створенні:
            // виклик ідемпотентний, уже доданих він не чіпає.
            await _rosterService.AutoFillAsync(match.Id);

            return await GetByIdAsync(match.Id)
                ?? throw new EntityNotFoundException("Match", match.Id);
        }

        public async Task<MatchDto> CreateAsync(CreateMatchDto createDto, int requestingUserId)
        {
            var homeTeamId = await ResolveHomeTeamIdAsync(createDto, requestingUserId);

            if (homeTeamId == createDto.AwayTeamId)
            {
                throw new BusinessLogicException("Команда не може грати сама із собою");
            }

            var match = _mapper.Map<Match>(createDto);
            match.HomeTeamId = homeTeamId;
            match.CreatedAt = DateTime.UtcNow;
            match.Status = MatchStatus.Scheduled;

            if (createDto.TournamentId == null)
            {
                // Товариський матч: турніру немає, тож дисципліну задає той, хто
                // його створює. Round = 0 і GroupStage — та сама позначка, що її
                // ставить MatchChallengeService: саме вона тримає товариські матчі
                // поза сіткою BracketService і поза підрахунком титулів.
                if (!Games.IsValid(createDto.Game))
                {
                    throw new BusinessLogicException("Оберіть дисципліну товариського матчу");
                }

                match.Game = createDto.Game!;
                match.Round = 0;
                match.MatchType = MatchTypes.GroupStage;
            }
            else
            {
                if (createDto.AwayTeamId == null)
                {
                    throw new BusinessLogicException(
                        "Турнірний матч не буває відкритим — вкажіть обидві команди");
                }

                var tournament = await _unitOfWork.Tournaments.GetByIdAsync(createDto.TournamentId.Value)
                    ?? throw new EntityNotFoundException("Tournament", createDto.TournamentId.Value);

                // Дисципліна успадковується від турніру, а не приходить від клієнта.
                match.Game = tournament.Game;
            }

            await _unitOfWork.Matches.AddAsync(match);
            await _unitOfWork.SaveChangesAsync();

            // Склад проставляється одразу: ростер матчу — це нормальний
            // стартовий стан, а не те, що організатор має згадати заповнити.
            // Кнопка «Заповнити зі складів» лишається, щоб підтягнути новачків
            // після трансферу — повторний виклик уже доданих не чіпає.
            await _rosterService.AutoFillAsync(match.Id);

            return _mapper.Map<MatchDto>(match);
        }

        /// <summary>
        /// Правка матчу. Оскільки DTO несе Status і WinnerTeamId, цей метод —
        /// другий шлях змінити результат уже завершеного матчу, крім
        /// CompleteMatchAsync. Саме через нього рейтинг і лічильники гравців
        /// колись розходилися з фактом: журнал лишався з попереднім
        /// результатом, а лічильники рахувалися вдруге. Тому після збереження
        /// результат зводиться так само, як після завершення — обидва боки
        /// цієї операції тепер ідемпотентні.
        /// </summary>
        public async Task<MatchDto?> UpdateAsync(int id, UpdateMatchDto updateDto)
        {
            var match = await _unitOfWork.Matches.GetByIdAsync(id);
            if (match == null)
                return null;

            var resultChanged =
                match.Status != updateDto.Status || match.WinnerTeamId != updateDto.WinnerTeamId;

            _mapper.Map(updateDto, match);

            // Час завершення має йти за статусом, інакше виправлений матч
            // лишається «завершеним» без дати, а backfill сортує за нею.
            if (match.Status == MatchStatus.Completed && match.EndedAt == null)
            {
                match.EndedAt = DateTime.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync();

            if (resultChanged)
            {
                await _rosterService.ApplyMatchResultAsync(match);
            }

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
                    m.AwayTeam == null ? (int?)null : m.AwayTeam.CaptainId))
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

            // Відкритий матч зіграти нема з ким. Без цієї перевірки він міг би
            // дійти до Completed із порожнім гостем — а на статус Completed
            // спираються геть усі підсумки.
            if (match is { AwayTeamId: null })
            {
                throw new BusinessLogicException(
                    "До матчу ще ніхто не приєднався — завершувати нема чого");
            }

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
