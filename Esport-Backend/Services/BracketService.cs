using TForge.Common;
using TForge.Data.Interfaces;
using TForge.Exceptions;
using TForge.Models;
using TForge.Services.Interfaces;

namespace TForge.Services
{
    /// <summary>
    /// Генерація та просування турнірної сітки на вибування (single elimination).
    ///
    /// Раунд 1 може бути кваліфікаційним (play-in), якщо кількість команд не є степенем двійки:
    /// грають лише 2*(n - p) команд, де p — найбільший степінь двійки, менший за n.
    /// Решта (2p - n) команд проходить далі без гри й приєднується у раунді 2,
    /// тому кожен наступний раунд уже має рівно p, p/2, ... команд.
    /// </summary>
    public class BracketService : IBracketService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMatchRosterService _rosterService;
        private readonly ILogger<BracketService> _logger;

        public BracketService(
            IUnitOfWork unitOfWork,
            IMatchRosterService rosterService,
            ILogger<BracketService> logger)
        {
            _unitOfWork = unitOfWork;
            _rosterService = rosterService;
            _logger = logger;
        }

        public async Task<int> GenerateAsync(int tournamentId)
        {
            var tournament = await _unitOfWork.Tournaments.GetWithTeamsAsync(tournamentId)
                ?? throw new EntityNotFoundException("Tournament", tournamentId);

            if (tournament.Status != TournamentStatus.Registration)
            {
                throw new BusinessLogicException(
                    "Сітку можна згенерувати лише поки турнір у статусі реєстрації");
            }

            var existingMatches = await _unitOfWork.Matches.GetByTournamentAsync(tournamentId);
            if (existingMatches.Any(m => m.Round > 0))
            {
                throw new BusinessLogicException("Сітку для цього турніру вже згенеровано");
            }

            var teams = tournament.Teams.OrderBy(t => t.Id).ToList();
            if (teams.Count < 2)
            {
                throw new BusinessLogicException(
                    "Для генерації сітки потрібно щонайменше 2 зареєстровані команди");
            }

            var pairs = BuildFirstRoundPairs(teams);
            // Якщо грають усі команди — це повноцінний раунд; інакше це кваліфікація
            var isFullRound = pairs.Count * 2 == teams.Count;
            var roundType = isFullRound ? MatchTypes.ForRoundSize(teams.Count) : MatchTypes.PlayIn;

            var matches = pairs.Select((pair, index) => new Match
            {
                TournamentId = tournamentId,
                HomeTeamId = pair.Home.Id,
                AwayTeamId = pair.Away.Id,
                ScheduledAt = tournament.StartDate.AddHours(index),
                Status = MatchStatus.Scheduled,
                MatchType = roundType,
                Game = tournament.Game,
                Format = "BO3",
                Round = 1,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            await _unitOfWork.Matches.AddRangeAsync(matches);

            tournament.Status = TournamentStatus.InProgress;
            tournament.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Tournaments.Update(tournament);

            await _unitOfWork.SaveChangesAsync();

            await FillRostersAsync(matches);

            _logger.LogInformation(
                "Згенеровано сітку турніру {TournamentId}: {Teams} команд, {Matches} матчів у раунді 1",
                tournamentId, teams.Count, matches.Count);

            return matches.Count;
        }

        /// <summary>
        /// Класичне посіювання: найсильніші (за порядком реєстрації) отримують bye,
        /// решта грає між собою за схемою «перший проти останнього».
        /// </summary>
        private static List<(Team Home, Team Away)> BuildFirstRoundPairs(List<Team> teams)
        {
            var n = teams.Count;
            var playInMatches = IsPowerOfTwo(n) ? n / 2 : n - LargestPowerOfTwoBelow(n);
            var playingTeams = teams.Skip(n - playInMatches * 2).ToList();

            var pairs = new List<(Team, Team)>();
            for (var i = 0; i < playInMatches; i++)
            {
                pairs.Add((playingTeams[i], playingTeams[playingTeams.Count - 1 - i]));
            }

            return pairs;
        }

        /// <summary>
        /// Викликається після завершення матчу сітки. Коли всі матчі раунду завершені,
        /// створює наступний раунд із переможців і команд, що пройшли без гри.
        /// </summary>
        public async Task AdvanceAsync(Match completedMatch)
        {
            // Товариський матч створюється з Round = 0 і без турніру, тож обидві
            // умови його відсікають — сітки він не стосується.
            if (completedMatch.Round <= 0 || completedMatch.TournamentId == null)
            {
                return; // матч поза сіткою або товариський
            }

            var tournamentId = completedMatch.TournamentId.Value;

            var all = (await _unitOfWork.Matches.GetByTournamentAsync(tournamentId)).ToList();
            var currentRound = all.Where(m => m.Round == completedMatch.Round).ToList();

            if (currentRound.Any(m => m.Status != MatchStatus.Completed))
            {
                return; // раунд ще триває
            }

            if (currentRound.Any(m => m.WinnerTeamId == null))
            {
                throw new BusinessLogicException(
                    "У матчі сітки потрібно вказати команду-переможця");
            }

            var tournament = await _unitOfWork.Tournaments.GetWithTeamsAsync(tournamentId);
            if (tournament == null)
            {
                return;
            }

            // Учасники наступного раунду: переможці цього раунду + ті, хто ще не грав (bye)
            var playedTeamIds = all
                .Where(m => m.Round > 0 && m.Round <= completedMatch.Round)
                .SelectMany(m => new[] { m.HomeTeamId, m.AwayTeamId })
                .ToHashSet();

            var winners = currentRound
                .OrderBy(m => m.Id)
                .Select(m => m.WinnerTeamId!.Value)
                .ToList();

            var byes = tournament.Teams
                .Where(t => !playedTeamIds.Contains(t.Id))
                .OrderBy(t => t.Id)
                .Select(t => t.Id)
                .ToList();

            var advancing = byes.Concat(winners).ToList();

            if (advancing.Count <= 1)
            {
                tournament.Status = TournamentStatus.Completed;
                tournament.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Tournaments.Update(tournament);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Турнір {TournamentId} завершено, переможець — команда {TeamId}",
                    tournament.Id, advancing.FirstOrDefault());
                return;
            }

            var nextRound = completedMatch.Round + 1;
            var roundType = MatchTypes.ForRoundSize(advancing.Count);
            var scheduleFrom = currentRound.Max(m => m.EndedAt ?? m.ScheduledAt).AddDays(1);

            var nextMatches = new List<Match>();
            for (var i = 0; i < advancing.Count / 2; i++)
            {
                nextMatches.Add(new Match
                {
                    TournamentId = tournament.Id,
                    HomeTeamId = advancing[i * 2],
                    AwayTeamId = advancing[i * 2 + 1],
                    ScheduledAt = scheduleFrom.AddHours(i),
                    Status = MatchStatus.Scheduled,
                    MatchType = roundType,
                    Game = tournament.Game,
                    Format = "BO3",
                    Round = nextRound,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _unitOfWork.Matches.AddRangeAsync(nextMatches);
            await _unitOfWork.SaveChangesAsync();

            await FillRostersAsync(nextMatches);

            _logger.LogInformation(
                "Турнір {TournamentId}: створено раунд {Round} ({Count} матчів)",
                tournament.Id, nextRound, nextMatches.Count);
        }

        /// <summary>
        /// Склади проставляються одразу після створення матчів сітки — так само,
        /// як для матчу, створеного вручну. Інакше половина розкладу турніру
        /// приходила б із порожнім ростером.
        /// </summary>
        private async Task FillRostersAsync(IEnumerable<Match> matches)
        {
            foreach (var match in matches)
            {
                await _rosterService.AutoFillAsync(match.Id);
            }
        }

        private static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;

        private static int LargestPowerOfTwoBelow(int value)
        {
            var power = 1;
            while (power * 2 < value)
            {
                power *= 2;
            }

            return power;
        }
    }
}
