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
        private readonly IRatingService _ratingService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IMapper _mapper;

        public PlayerService(
            IUnitOfWork unitOfWork,
            IStandingsService standingsService,
            IRatingService ratingService,
            IPasswordHasher passwordHasher,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _standingsService = standingsService;
            _ratingService = ratingService;
            _passwordHasher = passwordHasher;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PlayerDto>> GetAllAsync()
        {
            var players = await _unitOfWork.Players.GetAllAsync();
            return _mapper.Map<IEnumerable<PlayerDto>>(players);
        }

        public async Task<PagedResponse<PlayerDto>> GetPagedAsync(PagedRequest request, PlayerFilter? filter = null)
        {
            var query = ApplyPlayerFilters(
                _unitOfWork.Players.GetQueryable().Include(p => p.Team).Include(p => p.User),
                filter);

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

        /// <summary>Фільтри списку гравців — одне визначення для обох запитів.</summary>
        private static IQueryable<Player> ApplyPlayerFilters(IQueryable<Player> query, PlayerFilter? filter)
        {
            if (filter == null)
            {
                return query;
            }

            if (!string.IsNullOrEmpty(filter.Position))
                query = query.Where(p => p.Position == filter.Position);

            if (!string.IsNullOrEmpty(filter.Country))
                query = query.Where(p => p.Country == filter.Country);

            if (filter.MinAge.HasValue)
                query = query.Where(p => p.Age >= filter.MinAge.Value);

            if (filter.MaxAge.HasValue)
                query = query.Where(p => p.Age <= filter.MaxAge.Value);

            if (filter.TeamId.HasValue)
                query = query.Where(p => p.TeamId == filter.TeamId.Value);

            if (filter.IsActive.HasValue)
                query = query.Where(p => p.IsActive == filter.IsActive.Value);

            if (filter.FreeAgents.HasValue && filter.FreeAgents.Value)
                query = query.Where(p => p.TeamId == null);

            if (filter.UserId.HasValue)
                query = query.Where(p => p.UserId == filter.UserId.Value);

            return query;
        }

        /// <summary>
        /// Сторінка списку гравців. Показники рахує база кореляційними підзапитами,
        /// тож сортування діє на весь набір, а не лише на видиму сторінку.
        /// Умова «завершений матч із переможцем» збігається з PlayerRecordCalculator,
        /// тому список не розходиться з профілем гравця.
        /// </summary>
        public async Task<PagedResponse<PlayerRowDto>> GetPagedRowsAsync(
            PagedRequest request, PlayerFilter? filter = null)
        {
            var query = ApplyPlayerFilters(_unitOfWork.Players.GetQueryable(), filter);

            if (!string.IsNullOrEmpty(request.Search))
            {
                query = query.Where(p => p.Nickname.Contains(request.Search));
            }

            // Захоплюємо запит окремою змінною: EF перекладає корельований
            // підзапит по DbSet того самого контексту, але тільки якщо це
            // готовий IQueryable, а не виклик методу всередині дерева виразів.
            var ratings = _unitOfWork.PlayerRatings.GetQueryable();

            var rows = query.Select(p => new PlayerRowDto
            {
                Id = p.Id,
                UserId = p.UserId,
                Nickname = p.Nickname,
                Position = p.Position,
                Country = p.Country,
                IsActive = p.IsActive,
                AvatarUrl = p.User.AvatarPath,
                TeamId = p.TeamId,
                TeamName = p.Team != null ? p.Team.Name : null,
                Matches = p.MatchPlayers.Count(mp =>
                    mp.Match.Status == MatchStatus.Completed && mp.Match.WinnerTeamId != null),
                Wins = p.MatchPlayers.Count(mp =>
                    mp.Match.Status == MatchStatus.Completed
                    && mp.Match.WinnerTeamId != null
                    && mp.Match.WinnerTeamId == mp.TeamId),
                Kills = p.MatchPlayers.Sum(mp => mp.Kills),
                Deaths = p.MatchPlayers.Sum(mp => mp.Deaths),
                Assists = p.MatchPlayers.Sum(mp => mp.Assists),
                // Рейтинг ведеться окремо для кожної дисципліни, а в рядку
                // списку є місце лише для одного числа — показуємо найкраще.
                // Гравець без турнірних матчів рядка рейтингу не має, тож тут
                // буде null, і список покаже «—», а не вигадану тисячу.
                Rating = ratings
                    .Where(r => r.PlayerId == p.Id)
                    .OrderByDescending(r => r.Rating)
                    .Select(r => (int?)r.Rating)
                    .FirstOrDefault(),
                RatingGame = ratings
                    .Where(r => r.PlayerId == p.Id)
                    .OrderByDescending(r => r.Rating)
                    .Select(r => r.Game)
                    .FirstOrDefault()
            });

            rows = ApplyPlayerSort(rows, request.SortBy, request.SortDirection);

            var totalCount = await rows.CountAsync();
            var data = await rows.ApplyPaging(request).ToListAsync();

            // Похідні від уже вибраних чисел — рахуємо в памʼяті, щоб не
            // дублювати ті самі підзапити в SQL ще раз.
            foreach (var row in data)
            {
                row.Losses = row.Matches - row.Wins;
                row.WinRate = row.Matches == 0
                    ? 0
                    : Math.Round((decimal)row.Wins / row.Matches * 100, 1);
                row.Kda = Math.Round((row.Kills + row.Assists) / (double)Math.Max(1, row.Deaths), 2);
                row.RatingTier = row.Rating == null ? null : EloCalculator.Tier(row.Rating.Value);
            }

            return new PagedResponse<PlayerRowDto>
            {
                Data = data,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        /// <summary>
        /// Сортування за білим списком ключів. Невідомий ключ — типовий порядок,
        /// а не виняток: URL може прийти з чужого посилання.
        /// Обчислені колонки сортуються тими самими виразами, з яких показані
        /// числа, тож колонка завжди впорядковує саме те, що видно.
        /// </summary>
        private static IQueryable<PlayerRowDto> ApplyPlayerSort(
            IQueryable<PlayerRowDto> rows, string? sortBy, string? direction)
        {
            var descending = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);

            return sortBy switch
            {
                PlayerSortKeys.Nickname => descending
                    ? rows.OrderByDescending(r => r.Nickname)
                    : rows.OrderBy(r => r.Nickname),
                PlayerSortKeys.Position => descending
                    ? rows.OrderByDescending(r => r.Position)
                    : rows.OrderBy(r => r.Position),
                PlayerSortKeys.Country => descending
                    ? rows.OrderByDescending(r => r.Country)
                    : rows.OrderBy(r => r.Country),
                PlayerSortKeys.Team => descending
                    ? rows.OrderByDescending(r => r.TeamName)
                    : rows.OrderBy(r => r.TeamName),
                PlayerSortKeys.Matches => descending
                    ? rows.OrderByDescending(r => r.Matches)
                    : rows.OrderBy(r => r.Matches),
                PlayerSortKeys.Wins => descending
                    ? rows.OrderByDescending(r => r.Wins)
                    : rows.OrderBy(r => r.Wins),
                PlayerSortKeys.WinRate => descending
                    ? rows.OrderByDescending(r => r.Matches == 0 ? 0 : (double)r.Wins / r.Matches)
                    : rows.OrderBy(r => r.Matches == 0 ? 0 : (double)r.Wins / r.Matches),
                PlayerSortKeys.Kda => descending
                    ? rows.OrderByDescending(r => (r.Kills + r.Assists) / (double)(r.Deaths == 0 ? 1 : r.Deaths))
                    : rows.OrderBy(r => (r.Kills + r.Assists) / (double)(r.Deaths == 0 ? 1 : r.Deaths)),
                // Без рейтингу — це нуль, а не «невідомо»: інакше Postgres
                // ставив би NULL першими при спаданні, і на чолі драбини
                // опинилися б саме ті, хто не зіграв жодного турнірного матчу.
                PlayerSortKeys.Rating => descending
                    ? rows.OrderByDescending(r => r.Rating ?? 0)
                    : rows.OrderBy(r => r.Rating ?? 0),
                // Типовий порядок повторює колишню таблицю лідерів: за KDA, потім за фрагами.
                _ => rows
                    .OrderByDescending(r => (r.Kills + r.Assists) / (double)(r.Deaths == 0 ? 1 : r.Deaths))
                    .ThenByDescending(r => r.Kills)
            };
        }

        /// <summary>Профіль гравця за id користувача. Потрібен ендпоінту /players/me.</summary>
        public async Task<PlayerDto?> GetByUserIdAsync(int userId)
        {
            var player = await _unitOfWork.Players.GetQueryable()
                .Include(p => p.Team)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            return player != null ? _mapper.Map<PlayerDto>(player) : null;
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
            var ratings = await _ratingService.GetPlayerRatingsAsync(id);

            return new PlayerProfileDto
            {
                Player = _mapper.Map<PlayerDto>(player),
                Ratings = ratings.ToList(),
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

        /// <summary>
        /// Створює обліковий запис і профіль гравця однією транзакцією.
        /// Доступно лише адміністраторові — перевірка ролі на рівні контролера.
        /// </summary>
        public async Task<PlayerDto> CreateFullAsync(CreateFullPlayerDto createDto)
        {
            if (await _unitOfWork.Users.GetByUsernameAsync(createDto.Username) != null)
            {
                throw new BusinessLogicException("Користувач з таким іменем вже існує");
            }

            if (await _unitOfWork.Users.GetByEmailAsync(createDto.Email) != null)
            {
                throw new BusinessLogicException("Користувач з такою поштою вже існує");
            }

            var user = new User
            {
                Username = createDto.Username,
                Email = createDto.Email,
                FirstName = createDto.FirstName,
                LastName = createDto.LastName,
                Role = UserRoles.Player,
                PasswordHash = _passwordHasher.Hash(createDto.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var player = new Player
            {
                Nickname = createDto.Nickname,
                Position = createDto.Position,
                Country = createDto.Country,
                Age = createDto.Age,
                Ranking = 9999,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                player.UserId = user.Id;
                await _unitOfWork.Players.AddAsync(player);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

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
