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
    /// Перекладає дуель або склад матчу на входи моделі. Сам нічого не рахує:
    /// ознаки — ChessFeatures, дошки — ChessBoards, колір — SideOutcome.
    /// Прогноз рахується на читанні й ніде не зберігається.
    /// </summary>
    public class ChessPredictionService : IChessPredictionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IChessModelRunner _runner;

        public ChessPredictionService(IUnitOfWork unitOfWork, IChessModelRunner runner)
        {
            _unitOfWork = unitOfWork;
            _runner = runner;
        }

        private static PlayerGameProfile? ChessProfileOf(Player? player) =>
            player?.GameProfiles.FirstOrDefault(g => g.Game == Games.Chess);

        private static int? ChessRatingOf(Player? player)
        {
            var profile = ChessProfileOf(player);
            return profile == null ? null : ChessRatings.Effective(profile.Rating, profile.Rank);
        }

        public async Task<DuelPredictionDto> PredictDuelAsync(int duelId)
        {
            var duel = await _unitOfWork.Duels.GetQueryable()
                .AsNoTracking()
                .Include(d => d.ChallengerPlayer).ThenInclude(p => p.GameProfiles)
                .Include(d => d.OpponentPlayer!).ThenInclude(p => p.GameProfiles)
                .FirstOrDefaultAsync(d => d.Id == duelId)
                ?? throw new EntityNotFoundException("Duel", duelId);

            var timeControl = duel.TimeControl ?? TimeControls.Default;
            var dto = new DuelPredictionDto { TimeControl = timeControl, WhitePlayerId = duel.WhitePlayerId };

            if (duel.Game != Games.Chess) return Unavailable(dto, "Прогноз доступний лише для шахів");
            if (duel.Status is DuelStatuses.Completed or DuelStatuses.Cancelled or DuelStatuses.Declined)
                return Unavailable(dto, "Дуель уже завершена або не відбудеться");
            if (duel.OpponentPlayer == null) return Unavailable(dto, "Суперника ще не визначено");
            if (!_runner.IsAvailable) return Unavailable(dto, "Моделі прогнозу не завантажено");

            var challenger = ChessRatingOf(duel.ChallengerPlayer);
            var opponent = ChessRatingOf(duel.OpponentPlayer);
            dto.ChallengerRating = challenger;
            dto.OpponentRating = opponent;

            if (challenger is not int c || opponent is not int o)
                return Unavailable(dto, "Хтось із гравців не вказав шаховий рейтинг");

            var challengerWhite = _runner.Predict(c, o, timeControl);
            var challengerBlack = _runner.Predict(o, c, timeControl);

            foreach (var model in PredictionModels.All)
            {
                var asWhite = SideOutcome.From(challengerWhite[model], firstIsWhite: true);
                var asBlack = SideOutcome.From(challengerBlack[model], firstIsWhite: false);

                // Колір невідомий (дуель прийнято до появи жеребу) — чесно
                // усереднюємо обидва варіанти, а не вигадуємо його.
                var side = duel.WhitePlayerId == null
                    ? SideOutcome.Average(asWhite, asBlack)
                    : duel.WhitePlayerId == duel.ChallengerPlayerId ? asWhite : asBlack;

                dto.Outcomes.Add(ToDto(model, side));
            }

            dto.Available = true;
            dto.LittleData = _runner.Info!.HasLittleDataFor(timeControl);
            dto.ModelInfo = InfoDto();
            return dto;
        }

        public async Task<MatchPredictionDto> PredictMatchAsync(int matchId)
        {
            var match = await _unitOfWork.Matches.GetQueryable()
                .AsNoTracking()
                .Include(m => m.Tournament)
                .Include(m => m.MatchPlayers).ThenInclude(mp => mp.Player).ThenInclude(p => p.GameProfiles)
                .FirstOrDefaultAsync(m => m.Id == matchId)
                ?? throw new EntityNotFoundException("Match", matchId);

            var timeControl = match.Tournament?.TimeControl ?? TimeControls.Default;
            var dto = new MatchPredictionDto { TimeControl = timeControl };

            if (match.Game != Games.Chess) return Unavailable(dto, "Прогноз доступний лише для шахів");
            if (match.Status is MatchStatus.Completed or MatchStatus.Cancelled)
                return Unavailable(dto, "Матч завершено або скасовано");
            if (match.AwayTeamId is not int awayTeamId) return Unavailable(dto, "Суперника ще не визначено");
            if (!_runner.IsAvailable) return Unavailable(dto, "Моделі прогнозу не завантажено");

            // Склад — із MatchPlayer.TeamId, а не з поточної команди гравця:
            // та сама причина, що й у статистиці — трансфер не переписує матч.
            List<BoardCandidate> Side(int teamId) => match.MatchPlayers
                .Where(mp => mp.TeamId == teamId)
                .Select(mp => (mp.Player, Rating: ChessRatingOf(mp.Player)))
                .Where(x => x.Rating.HasValue)
                .Select(x => new BoardCandidate(
                    x.Player.Id,
                    x.Player.Nickname,
                    ChessProfileOf(x.Player)?.Position ?? string.Empty,
                    x.Rating!.Value))
                .ToList();

            var boards = ChessBoards.Pair(Side(match.HomeTeamId), Side(awayTeamId));
            if (boards.Count == 0) return Unavailable(dto, "У складах немає гравців із шаховим рейтингом");

            var totals = PredictionModels.All.ToDictionary(m => m, _ => (Home: 0.0, Away: 0.0));

            foreach (var board in boards)
            {
                var white = board.HomeIsWhite ? board.Home.Rating : board.Away.Rating;
                var black = board.HomeIsWhite ? board.Away.Rating : board.Home.Rating;
                var outcomes = _runner.Predict(white, black, timeControl);

                var boardDto = new BoardPredictionDto
                {
                    Board = board.Board,
                    HomeNickname = board.Home.Nickname,
                    AwayNickname = board.Away.Nickname,
                    HomeRating = board.Home.Rating,
                    AwayRating = board.Away.Rating,
                    HomeIsWhite = board.HomeIsWhite
                };

                foreach (var model in PredictionModels.All)
                {
                    var side = SideOutcome.From(outcomes[model], board.HomeIsWhite);
                    boardDto.Outcomes.Add(ToDto(model, side));
                    var t = totals[model];
                    totals[model] = (t.Home + side.FirstExpectedPoints, t.Away + side.SecondExpectedPoints);
                }

                dto.Boards.Add(boardDto);
            }

            dto.Totals = PredictionModels.All
                .Select(m => new ExpectedPointsDto { Model = m, Home = totals[m].Home, Away = totals[m].Away })
                .ToList();
            dto.Available = true;
            dto.LittleData = _runner.Info!.HasLittleDataFor(timeControl);
            dto.ModelInfo = InfoDto();
            return dto;
        }

        private static ModelOutcomeDto ToDto(string model, SideOutcome s) =>
            new() { Model = model, FirstWin = s.FirstWin, Draw = s.Draw, SecondWin = s.SecondWin };

        private PredictionModelInfoDto? InfoDto()
        {
            var info = _runner.Info;
            if (info == null) return null;

            return new PredictionModelInfoDto
            {
                TrainedAt = info.TrainedAt,
                Dataset = info.Dataset,
                RowsTrain = info.RowsTrain,
                Metrics = PredictionModels.All
                    .Where(info.Metrics.ContainsKey)
                    .Select(m => new ModelMetricsDto
                    {
                        Model = m,
                        LogLoss = info.Metrics[m].LogLoss,
                        Brier = info.Metrics[m].Brier,
                        Accuracy = info.Metrics[m].Accuracy
                    })
                    .ToList()
            };
        }

        private static DuelPredictionDto Unavailable(DuelPredictionDto dto, string reason)
        {
            dto.Available = false;
            dto.Reason = reason;
            return dto;
        }

        private static MatchPredictionDto Unavailable(MatchPredictionDto dto, string reason)
        {
            dto.Available = false;
            dto.Reason = reason;
            return dto;
        }
    }
}
