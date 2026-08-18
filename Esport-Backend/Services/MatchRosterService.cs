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
    public class MatchRosterService : IMatchRosterService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRatingService _ratingService;
        private readonly IMapper _mapper;

        public MatchRosterService(IUnitOfWork unitOfWork, IRatingService ratingService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _ratingService = ratingService;
            _mapper = mapper;
        }

        public async Task<IEnumerable<MatchPlayerDto>> GetRosterAsync(int matchId)
        {
            await EnsureMatchExistsAsync(matchId);
            return _mapper.Map<IEnumerable<MatchPlayerDto>>(await LoadRosterAsync(matchId));
        }

        /// <summary>
        /// Заповнює ростер активними гравцями обох команд. Уже доданих не чіпає,
        /// тож повторний виклик безпечно підтягує новачків складу.
        /// </summary>
        public async Task<IEnumerable<MatchPlayerDto>> AutoFillAsync(int matchId)
        {
            var match = await EnsureMatchExistsAsync(matchId);

            var existingPlayerIds = (await LoadRosterAsync(matchId))
                .Select(mp => mp.PlayerId)
                .ToHashSet();

            var squad = await _unitOfWork.Players.GetQueryable()
                .Where(p => p.IsActive && p.TeamId != null &&
                            (p.TeamId == match.HomeTeamId || p.TeamId == match.AwayTeamId))
                .ToListAsync();

            var added = squad
                .Where(player => !existingPlayerIds.Contains(player.Id))
                .Select(player => new MatchPlayer
                {
                    MatchId = matchId,
                    PlayerId = player.Id,
                    // Фіксуємо команду саме на момент матчу
                    TeamId = player.TeamId!.Value,
                    IsStarter = true
                })
                .ToList();

            if (added.Count > 0)
            {
                await _unitOfWork.MatchPlayers.AddRangeAsync(added);
                await _unitOfWork.SaveChangesAsync();
            }

            return _mapper.Map<IEnumerable<MatchPlayerDto>>(await LoadRosterAsync(matchId));
        }

        public async Task<MatchPlayerDto> AddPlayerAsync(int matchId, CreateMatchPlayerDto dto)
        {
            var match = await EnsureMatchExistsAsync(matchId);

            var player = await _unitOfWork.Players.GetByIdAsync(dto.PlayerId)
                ?? throw new EntityNotFoundException("Player", dto.PlayerId);

            if (player.TeamId != match.HomeTeamId && player.TeamId != match.AwayTeamId)
            {
                throw new BusinessLogicException("Гравець не належить до жодної з команд цього матчу");
            }

            var alreadyInRoster = await _unitOfWork.MatchPlayers.GetQueryable()
                .AnyAsync(mp => mp.MatchId == matchId && mp.PlayerId == dto.PlayerId);

            if (alreadyInRoster)
            {
                throw new BusinessLogicException("Гравець уже є в ростері цього матчу");
            }

            var entry = new MatchPlayer
            {
                MatchId = matchId,
                PlayerId = dto.PlayerId,
                TeamId = player.TeamId!.Value,
                Kills = dto.Kills,
                Deaths = dto.Deaths,
                Assists = dto.Assists,
                Champion = dto.Champion,
                IsStarter = dto.IsStarter
            };

            await _unitOfWork.MatchPlayers.AddAsync(entry);
            await _unitOfWork.SaveChangesAsync();

            var saved = (await LoadRosterAsync(matchId)).First(mp => mp.Id == entry.Id);
            return _mapper.Map<MatchPlayerDto>(saved);
        }

        public async Task<MatchPlayerDto> UpdateEntryAsync(int matchId, int entryId, UpdateMatchPlayerDto dto)
        {
            var entry = await _unitOfWork.MatchPlayers.GetQueryable()
                .FirstOrDefaultAsync(mp => mp.Id == entryId && mp.MatchId == matchId)
                ?? throw new EntityNotFoundException("MatchPlayer", entryId);

            entry.Kills = dto.Kills;
            entry.Deaths = dto.Deaths;
            entry.Assists = dto.Assists;
            entry.Champion = dto.Champion;
            entry.IsStarter = dto.IsStarter;

            await _unitOfWork.SaveChangesAsync();

            var saved = (await LoadRosterAsync(matchId)).First(mp => mp.Id == entryId);
            return _mapper.Map<MatchPlayerDto>(saved);
        }

        public async Task RemoveEntryAsync(int matchId, int entryId)
        {
            var entry = await _unitOfWork.MatchPlayers.GetQueryable()
                .FirstOrDefaultAsync(mp => mp.Id == entryId && mp.MatchId == matchId)
                ?? throw new EntityNotFoundException("MatchPlayer", entryId);

            _unitOfWork.MatchPlayers.Remove(entry);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Переносить результат завершеного матчу в карʼєрну статистику гравців.
        /// Якщо ростер не заповнювали, створюємо його зараз — так MatchPlayer лишається
        /// повним записом участі, а поточний склад команди не переписує минуле.
        ///
        /// Тут же нараховується рейтинг: це єдина точка, у якій завершений матч
        /// перетворюється на карʼєрні показники, і кличуть її з одного місця
        /// (MatchService.CompleteMatchAsync), тож другого шляху завершити матч
        /// без рейтингу не існує. Порядок не випадковий — рейтинг роздається
        /// по рядках ростера, а вони мають існувати на цей момент.
        /// </summary>
        /// <summary>
        /// Зводить кеш лічильників гравця та журнал рейтингу з фактичним
        /// результатом матчу.
        ///
        /// Лічильники не додаються по одиниці, а перераховуються з рядків
        /// ростера. Додавання було правильним рівно один раз — при першому
        /// завершенні матчу; правка складу після завершення або повторне
        /// проставляння результату через PUT розсинхронізовували кеш із
        /// похідною статистикою, яку показують списки. Перерахунок робить
        /// метод ідемпотентним, тож розійтися вже нема з чим.
        ///
        /// Виклик лишається і тоді, коли матч перестав бути завершеним:
        /// журнал рейтингу тоді сторнує нарахування, а лічильники впадуть
        /// самі, бо цей матч більше не потрапляє в підрахунок.
        /// </summary>
        public async Task ApplyMatchResultAsync(Match match)
        {
            var roster = await LoadRosterAsync(match.Id);

            if (roster.Count == 0 && match.Status == MatchStatus.Completed && match.WinnerTeamId != null)
            {
                roster = await MaterialiseRosterAsync(match);
            }

            foreach (var playerId in roster.Select(entry => entry.PlayerId).Distinct())
            {
                await RecalculatePlayerRecordAsync(playerId);
            }

            await _unitOfWork.SaveChangesAsync();

            await _ratingService.RateMatchAsync(match);
        }

        /// <summary>
        /// Переписує кеш Player.TotalMatches/Wins/Losses/WinRate з рядків
        /// MatchPlayer — єдиного джерела статистики гравця. Рахує той самий
        /// PlayerRecordCalculator, з якого читають профіль і списки, тож три
        /// місця не можуть дати трьох різних відповідей.
        /// </summary>
        private async Task RecalculatePlayerRecordAsync(int playerId)
        {
            var player = await _unitOfWork.Players.GetByIdAsync(playerId);
            if (player == null)
            {
                return;
            }

            var rows = await _unitOfWork.MatchPlayers.GetQueryable()
                .Include(mp => mp.Match)
                .Where(mp => mp.PlayerId == playerId)
                .ToListAsync();

            var record = PlayerRecordCalculator.Calculate(rows);

            player.TotalMatches = record.Matches;
            player.Wins = record.Wins;
            player.Losses = record.Losses;
            player.WinRate = record.WinRate;
        }

        /// <summary>Створює порожні рядки ростера для активних гравців обох команд.</summary>
        private async Task<List<MatchPlayer>> MaterialiseRosterAsync(Match match)
        {
            var squad = await _unitOfWork.Players.GetQueryable()
                .Where(p => p.IsActive && p.TeamId != null &&
                            (p.TeamId == match.HomeTeamId || p.TeamId == match.AwayTeamId))
                .ToListAsync();

            if (squad.Count == 0)
            {
                return new List<MatchPlayer>();
            }

            var created = squad.Select(player => new MatchPlayer
            {
                MatchId = match.Id,
                PlayerId = player.Id,
                TeamId = player.TeamId!.Value,
                IsStarter = true
            }).ToList();

            await _unitOfWork.MatchPlayers.AddRangeAsync(created);
            await _unitOfWork.SaveChangesAsync();

            return await LoadRosterAsync(match.Id);
        }

        private async Task<Match> EnsureMatchExistsAsync(int matchId)
        {
            return await _unitOfWork.Matches.GetByIdAsync(matchId)
                ?? throw new EntityNotFoundException("Match", matchId);
        }

        private async Task<List<MatchPlayer>> LoadRosterAsync(int matchId)
        {
            return await _unitOfWork.MatchPlayers.GetQueryable()
                .Include(mp => mp.Player)
                .ThenInclude(p => p.Team)
                .Include(mp => mp.Match)
                .Where(mp => mp.MatchId == matchId)
                .OrderBy(mp => mp.TeamId)
                .ThenBy(mp => mp.Player.Nickname)
                .ToListAsync();
        }
    }
}
