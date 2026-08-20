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

        /// <summary>
        /// Сторінка списку команд із показниками, порахованими базою.
        /// HomeMatches/AwayMatches працюють лише після виправлення тіньових
        /// зовнішніх ключів — доти ці навігації читали не ту колонку й давали
        /// хибні підсумки.
        /// </summary>
        public async Task<PagedResponse<TeamRowDto>> GetPagedRowsAsync(
            PagedRequest request, TeamFilter? filter = null)
        {
            IQueryable<Team> query = _unitOfWork.Teams.GetQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Name))
                    query = query.Where(t => t.Name.Contains(filter.Name));

                if (!string.IsNullOrEmpty(filter.Region))
                    query = query.Where(t => t.Region == filter.Region);

                if (filter.IsActive.HasValue)
                    query = query.Where(t => t.IsActive == filter.IsActive.Value);

                if (filter.CaptainId.HasValue)
                    query = query.Where(t => t.CaptainId == filter.CaptainId.Value);

                if (filter.TournamentId.HasValue)
                    query = query.Where(t => t.Tournaments.Any(tour => tour.Id == filter.TournamentId.Value));
            }

            if (!string.IsNullOrEmpty(request.Search))
            {
                query = query.Where(t => t.Name.Contains(request.Search) || t.Tag.Contains(request.Search));
            }

            // Захоплюємо запит окремою змінною: EF перекладає корельований
            // підзапит по DbSet того самого контексту, але тільки якщо це
            // готовий IQueryable, а не виклик методу всередині дерева виразів.
            var ratings = _unitOfWork.TeamRatings.GetQueryable();

            var rows = query.Select(t => new TeamRowDto
            {
                Id = t.Id,
                Name = t.Name,
                Tag = t.Tag,
                Region = t.Region,
                LogoPath = t.LogoPath,
                IsActive = t.IsActive,
                CaptainId = t.CaptainId,
                CaptainUsername = t.Captain != null ? t.Captain.Username : null,
                PlayerCount = t.Players.Count,
                Played = t.HomeMatches.Count(m =>
                             m.Status == MatchStatus.Completed && m.WinnerTeamId != null)
                         + t.AwayMatches.Count(m =>
                             m.Status == MatchStatus.Completed && m.WinnerTeamId != null),
                Wins = t.HomeMatches.Count(m =>
                           m.Status == MatchStatus.Completed && m.WinnerTeamId == t.Id)
                       + t.AwayMatches.Count(m =>
                           m.Status == MatchStatus.Completed && m.WinnerTeamId == t.Id),
                Titles = t.HomeMatches.Count(m =>
                             m.Status == MatchStatus.Completed
                             && m.WinnerTeamId == t.Id
                             && m.MatchType == MatchTypes.Final)
                         + t.AwayMatches.Count(m =>
                             m.Status == MatchStatus.Completed
                             && m.WinnerTeamId == t.Id
                             && m.MatchType == MatchTypes.Final),
                // Рейтинг ведеться окремо для кожної дисципліни, а в рядку
                // списку є місце лише для одного числа — показуємо найкраще.
                Rating = ratings
                    .Where(r => r.TeamId == t.Id)
                    .OrderByDescending(r => r.Rating)
                    .Select(r => (int?)r.Rating)
                    .FirstOrDefault(),
                RatingGame = ratings
                    .Where(r => r.TeamId == t.Id)
                    .OrderByDescending(r => r.Rating)
                    .Select(r => r.Game)
                    .FirstOrDefault()
            });

            rows = ApplyTeamSort(rows, request.SortBy, request.SortDirection);

            var totalCount = await rows.CountAsync();
            var data = await rows.ApplyPaging(request).ToListAsync();

            foreach (var row in data)
            {
                row.Losses = row.Played - row.Wins;
                row.WinRate = row.Played == 0
                    ? 0
                    : Math.Round((decimal)row.Wins / row.Played * 100, 1);
                row.RatingTier = row.Rating == null ? null : EloCalculator.Tier(row.Rating.Value);
            }

            return new PagedResponse<TeamRowDto>
            {
                Data = data,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        /// <summary>
        /// Сортування за білим списком ключів. Невідомий ключ — типовий порядок,
        /// що повторює колишню таблицю команд.
        /// </summary>
        private static IQueryable<TeamRowDto> ApplyTeamSort(
            IQueryable<TeamRowDto> rows, string? sortBy, string? direction)
        {
            var descending = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);

            return sortBy switch
            {
                TeamSortKeys.Name => descending
                    ? rows.OrderByDescending(r => r.Name)
                    : rows.OrderBy(r => r.Name),
                TeamSortKeys.Tag => descending
                    ? rows.OrderByDescending(r => r.Tag)
                    : rows.OrderBy(r => r.Tag),
                TeamSortKeys.Region => descending
                    ? rows.OrderByDescending(r => r.Region)
                    : rows.OrderBy(r => r.Region),
                TeamSortKeys.Played => descending
                    ? rows.OrderByDescending(r => r.Played)
                    : rows.OrderBy(r => r.Played),
                TeamSortKeys.Wins => descending
                    ? rows.OrderByDescending(r => r.Wins)
                    : rows.OrderBy(r => r.Wins),
                TeamSortKeys.WinRate => descending
                    ? rows.OrderByDescending(r => r.Played == 0 ? 0 : (double)r.Wins / r.Played)
                    : rows.OrderBy(r => r.Played == 0 ? 0 : (double)r.Wins / r.Played),
                TeamSortKeys.Titles => descending
                    ? rows.OrderByDescending(r => r.Titles)
                    : rows.OrderBy(r => r.Titles),
                // Без рейтингу — це нуль, а не «невідомо»: інакше Postgres
                // ставив би NULL першими при спаданні, і на чолі драбини
                // опинилися б команди, що не зіграли жодного турнірного матчу.
                TeamSortKeys.Rating => descending
                    ? rows.OrderByDescending(r => r.Rating ?? 0)
                    : rows.OrderBy(r => r.Rating ?? 0),
                _ => rows
                    .OrderByDescending(r => r.Titles)
                    .ThenByDescending(r => r.Wins)
                    .ThenBy(r => r.Name)
            };
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

        public async Task<TeamDto> CreateAsync(CreateTeamDto createDto, int captainId)
        {
            var team = _mapper.Map<Team>(createDto);
            team.CaptainId = captainId;
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

        /// <summary>
        /// Передає капітанство гравцеві цієї ж команди.
        ///
        /// Team.CaptainId посилається на User, а не на Player, тож новий капітан
        /// мусить мати акаунт: гравець, заведений організатором вручну, його не
        /// має й керувати командою не зможе. Це перевіряється тут, а не в
        /// політиці — політика вирішує лише, кому дозволено, а не кому можливо.
        /// </summary>
        public async Task<TeamDto> TransferCaptaincyAsync(
            int teamId, int playerId, int requestingUserId, bool isAdmin)
        {
            var team = await _unitOfWork.Teams.GetByIdAsync(teamId)
                ?? throw new EntityNotFoundException("Team", teamId);

            if (!TeamCaptaincyPolicy.CanTransfer(team.CaptainId, requestingUserId, isAdmin))
            {
                throw new ForbiddenException("Передати капітанство може лише чинний капітан команди");
            }

            var player = await _unitOfWork.Players.GetByIdAsync(playerId)
                ?? throw new EntityNotFoundException("Player", playerId);

            if (player.TeamId != teamId)
            {
                throw new BusinessLogicException("Капітаном може стати лише гравець цієї команди");
            }

            if (player.UserId == 0)
            {
                throw new BusinessLogicException(
                    "У цього гравця немає акаунта, тож він не може керувати командою");
            }

            if (team.CaptainId == player.UserId)
            {
                throw new BusinessLogicException("Цей гравець уже капітан команди");
            }

            team.CaptainId = player.UserId;
            _unitOfWork.Teams.Update(team);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<TeamDto>(await _unitOfWork.Teams.GetByIdAsync(teamId));
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
