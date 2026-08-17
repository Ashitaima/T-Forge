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
    /// Рейтингова драбина. Сервіс лише читає й пише рядки — уся арифметика
    /// живе в чистому EloCalculator, тож правила можна перевіряти без бази.
    /// </summary>
    public class RatingService : IRatingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RatingService> _logger;

        public RatingService(IUnitOfWork unitOfWork, ILogger<RatingService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task RateMatchAsync(Match match)
        {
            if (!EloCalculator.IsRated(match.TournamentId, match.Status, match.WinnerTeamId))
            {
                return;
            }

            if (!Games.IsValid(match.Game))
            {
                // Дисципліна проставляється з турніру, тож порожня означає
                // зіпсовані дані, а не звичайний випадок — рейтинг мовчки
                // пропускаємо, але лишаємо слід.
                _logger.LogWarning(
                    "Матч {MatchId} не отримав рейтингу: невідома дисципліна «{Game}»",
                    match.Id, match.Game);
                return;
            }

            // Журнал — це і є захист від подвійного нарахування. CompleteMatchAsync
            // уже не дає завершити матч удруге, але MatchService.UpdateAsync
            // проставляє Status і WinnerTeamId напряму, і цю дірку закриває саме
            // перевірка нижче разом з унікальним індексом (TeamId, MatchId).
            var alreadyRated = await _unitOfWork.TeamRatingChanges
                .ExistsAsync(c => c.MatchId == match.Id);

            if (alreadyRated)
            {
                return;
            }

            var home = await GetOrCreateTeamRatingAsync(match.HomeTeamId, match.Game);
            var away = await GetOrCreateTeamRatingAsync(match.AwayTeamId, match.Game);

            var homeWon = match.WinnerTeamId == match.HomeTeamId;

            // K рахується для кожної сторони окремо: ознайомчий період — це
            // властивість команди, а не матчу. Коли обидві його пройшли, приріст
            // переможця дорівнює втраті переможеного.
            var homeDelta = EloCalculator.Delta(
                home.Rating, away.Rating, homeWon, EloCalculator.KFactor(match.MatchType, home.MatchesRated));
            var awayDelta = EloCalculator.Delta(
                away.Rating, home.Rating, !homeWon, EloCalculator.KFactor(match.MatchType, away.MatchesRated));

            var homeChange = ApplyTeamChange(home, match, homeDelta);
            var awayChange = ApplyTeamChange(away, match, awayDelta);

            await _unitOfWork.TeamRatingChanges.AddRangeAsync(new[] { homeChange, awayChange });

            await RatePlayersAsync(match, homeDelta, awayDelta);

            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Гравці отримують той самий приріст, що й команда, за яку виступали.
        /// Без зважування на KDA: ці числа вносить руками той, хто завершує
        /// матч, тож масштабування за ними лише винагороджувало б накрутку.
        /// Команду беремо з MatchPlayer.TeamId — трансфер не переписує минуле.
        /// </summary>
        private async Task RatePlayersAsync(Match match, int homeDelta, int awayDelta)
        {
            var roster = await _unitOfWork.MatchPlayers.GetQueryable()
                .Where(mp => mp.MatchId == match.Id)
                .Select(mp => new { mp.PlayerId, mp.TeamId })
                .ToListAsync();

            foreach (var entry in roster)
            {
                var delta = entry.TeamId == match.HomeTeamId
                    ? homeDelta
                    : entry.TeamId == match.AwayTeamId ? awayDelta : (int?)null;

                if (delta == null)
                {
                    continue; // рядок ростера не належить жодній зі сторін матчу
                }

                var rating = await GetOrCreatePlayerRatingAsync(entry.PlayerId, match.Game);
                var before = rating.Rating;

                rating.Rating = EloCalculator.Apply(before, delta.Value);
                rating.Peak = Math.Max(rating.Peak, rating.Rating);
                rating.MatchesRated += 1;
                rating.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.PlayerRatingChanges.AddAsync(new PlayerRatingChange
                {
                    PlayerId = entry.PlayerId,
                    Game = match.Game,
                    MatchId = match.Id,
                    // Записуємо фактичний зсув, а не задуманий: біля підлоги
                    // вони розходяться, і графік має показувати те, що сталося.
                    Delta = rating.Rating - before,
                    RatingBefore = before,
                    RatingAfter = rating.Rating,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        private static TeamRatingChange ApplyTeamChange(TeamRating rating, Match match, int delta)
        {
            var before = rating.Rating;

            rating.Rating = EloCalculator.Apply(before, delta);
            rating.Peak = Math.Max(rating.Peak, rating.Rating);
            rating.MatchesRated += 1;
            rating.UpdatedAt = DateTime.UtcNow;

            return new TeamRatingChange
            {
                TeamId = rating.TeamId,
                Game = match.Game,
                MatchId = match.Id,
                Delta = rating.Rating - before,
                RatingBefore = before,
                RatingAfter = rating.Rating,
                CreatedAt = DateTime.UtcNow
            };
        }

        private async Task<TeamRating> GetOrCreateTeamRatingAsync(int teamId, string game)
        {
            var existing = await _unitOfWork.TeamRatings.GetQueryable()
                .FirstOrDefaultAsync(r => r.TeamId == teamId && r.Game == game);

            if (existing != null)
            {
                return existing;
            }

            var created = new TeamRating
            {
                TeamId = teamId,
                Game = game,
                Rating = EloCalculator.BaseRating,
                Peak = EloCalculator.BaseRating,
                MatchesRated = 0,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.TeamRatings.AddAsync(created);
            return created;
        }

        private async Task<PlayerRating> GetOrCreatePlayerRatingAsync(int playerId, string game)
        {
            var existing = await _unitOfWork.PlayerRatings.GetQueryable()
                .FirstOrDefaultAsync(r => r.PlayerId == playerId && r.Game == game);

            if (existing != null)
            {
                return existing;
            }

            var created = new PlayerRating
            {
                PlayerId = playerId,
                Game = game,
                Rating = EloCalculator.BaseRating,
                Peak = EloCalculator.BaseRating,
                MatchesRated = 0,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.PlayerRatings.AddAsync(created);
            return created;
        }

        /// <summary>
        /// Програє історію турнірних матчів тим самим калькулятором, що й жива
        /// гра. Через це драбина не стартує з усіх, рівних тисячі, а сам перегін
        /// заразом працює перевіркою: другої реалізації правил не існує.
        /// </summary>
        public async Task<int> BackfillAsync()
        {
            var ratedMatchIds = await _unitOfWork.TeamRatingChanges.GetQueryable()
                .Select(c => c.MatchId)
                .Distinct()
                .ToListAsync();

            var pending = await _unitOfWork.Matches.GetQueryable()
                .Where(m => m.TournamentId != null
                            && m.Status == MatchStatus.Completed
                            && m.WinnerTeamId != null
                            && !ratedMatchIds.Contains(m.Id))
                // Порядок важливий: рейтинг залежить від того, яким він був
                // до матчу, тож переграти можна лише хронологічно.
                .OrderBy(m => m.EndedAt ?? m.ScheduledAt)
                .ThenBy(m => m.Id)
                .ToListAsync();

            foreach (var match in pending)
            {
                await RateMatchAsync(match);
            }

            if (pending.Count > 0)
            {
                _logger.LogInformation("Нараховано рейтинг за {Count} матчів заднім числом", pending.Count);
            }

            return pending.Count;
        }

        public async Task<IEnumerable<RatingDto>> GetTeamRatingsAsync(int teamId)
        {
            var rows = await _unitOfWork.TeamRatings.GetQueryable()
                .Where(r => r.TeamId == teamId)
                .OrderByDescending(r => r.Rating)
                .ToListAsync();

            return rows.Select(r => ToDto(r.Game, r.Rating, r.Peak, r.MatchesRated, r.UpdatedAt));
        }

        public async Task<IEnumerable<RatingDto>> GetPlayerRatingsAsync(int playerId)
        {
            var rows = await _unitOfWork.PlayerRatings.GetQueryable()
                .Where(r => r.PlayerId == playerId)
                .OrderByDescending(r => r.Rating)
                .ToListAsync();

            return rows.Select(r => ToDto(r.Game, r.Rating, r.Peak, r.MatchesRated, r.UpdatedAt));
        }

        private static RatingDto ToDto(string game, int rating, int peak, int matchesRated, DateTime updatedAt) =>
            new()
            {
                Game = game,
                Rating = rating,
                Peak = peak,
                MatchesRated = matchesRated,
                Tier = EloCalculator.Tier(rating),
                UpdatedAt = updatedAt
            };

        public async Task<IEnumerable<RatingChangeDto>> GetTeamHistoryAsync(int teamId, string? game, int take)
        {
            var query = _unitOfWork.TeamRatingChanges.GetQueryable()
                .Include(c => c.Match).ThenInclude(m => m.HomeTeam)
                .Include(c => c.Match).ThenInclude(m => m.AwayTeam)
                .Include(c => c.Match).ThenInclude(m => m.Tournament)
                .Where(c => c.TeamId == teamId);

            if (!string.IsNullOrEmpty(game))
            {
                query = query.Where(c => c.Game == game);
            }

            var rows = await TakeLatestAsync(query, c => c.CreatedAt, c => c.Id, take);

            return rows.Select(c => new RatingChangeDto
            {
                MatchId = c.MatchId,
                Game = c.Game,
                Delta = c.Delta,
                RatingBefore = c.RatingBefore,
                RatingAfter = c.RatingAfter,
                CreatedAt = c.CreatedAt,
                OpponentName = c.Match.HomeTeamId == teamId
                    ? c.Match.AwayTeam?.Name
                    : c.Match.HomeTeam?.Name,
                TournamentName = c.Match.Tournament?.Name,
                MatchType = c.Match.MatchType
            });
        }

        public async Task<IEnumerable<RatingChangeDto>> GetPlayerHistoryAsync(int playerId, string? game, int take)
        {
            var query = _unitOfWork.PlayerRatingChanges.GetQueryable()
                .Include(c => c.Match).ThenInclude(m => m.HomeTeam)
                .Include(c => c.Match).ThenInclude(m => m.AwayTeam)
                .Include(c => c.Match).ThenInclude(m => m.Tournament)
                .Where(c => c.PlayerId == playerId);

            if (!string.IsNullOrEmpty(game))
            {
                query = query.Where(c => c.Game == game);
            }

            var rows = await TakeLatestAsync(query, c => c.CreatedAt, c => c.Id, take);

            // Суперника визначаємо за командою, у складі якої гравець вийшов
            // саме на цей матч, а не за його поточною командою.
            var teamByMatch = await _unitOfWork.MatchPlayers.GetQueryable()
                .Where(mp => mp.PlayerId == playerId)
                .Select(mp => new { mp.MatchId, mp.TeamId })
                .ToDictionaryAsync(x => x.MatchId, x => x.TeamId);

            return rows.Select(c => new RatingChangeDto
            {
                MatchId = c.MatchId,
                Game = c.Game,
                Delta = c.Delta,
                RatingBefore = c.RatingBefore,
                RatingAfter = c.RatingAfter,
                CreatedAt = c.CreatedAt,
                OpponentName = teamByMatch.TryGetValue(c.MatchId, out var playedFor)
                    && c.Match.HomeTeamId == playedFor
                        ? c.Match.AwayTeam?.Name
                        : c.Match.HomeTeam?.Name,
                TournamentName = c.Match.Tournament?.Name,
                MatchType = c.Match.MatchType
            });
        }

        /// <summary>
        /// Останні N змін, повернуті у хронологічному порядку: база віддає
        /// найсвіжіші, а графік читається зліва направо.
        /// </summary>
        private static async Task<List<T>> TakeLatestAsync<T>(
            IQueryable<T> query,
            System.Linq.Expressions.Expression<Func<T, DateTime>> byDate,
            System.Linq.Expressions.Expression<Func<T, int>> byId,
            int take)
        {
            var rows = await query
                .OrderByDescending(byDate)
                .ThenByDescending(byId)
                .Take(Math.Clamp(take, 1, 100))
                .ToListAsync();

            rows.Reverse();
            return rows;
        }

        public async Task<MatchRatingDeltaDto> GetMatchDeltaAsync(int matchId)
        {
            var match = await _unitOfWork.Matches.GetByIdAsync(matchId)
                ?? throw new EntityNotFoundException("Match", matchId);

            var changes = await _unitOfWork.TeamRatingChanges.GetQueryable()
                .Where(c => c.MatchId == matchId)
                .ToListAsync();

            return new MatchRatingDeltaDto
            {
                MatchId = matchId,
                Game = match.Game,
                HomeDelta = changes.FirstOrDefault(c => c.TeamId == match.HomeTeamId)?.Delta,
                AwayDelta = changes.FirstOrDefault(c => c.TeamId == match.AwayTeamId)?.Delta
            };
        }
    }
}
