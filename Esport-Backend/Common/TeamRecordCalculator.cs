using TForge.Models;

namespace TForge.Common
{
    /// <summary>
    /// Арифметика результатів команди. Чисті функції без EF та сервісів —
    /// саме тому їх можна перевіряти юніт-тестами без бази.
    /// </summary>
    public static class TeamRecordCalculator
    {
        public record TeamRecord(int Played, int Wins, int Losses, decimal WinRate);

        public record Streak(string Type, int Count);

        /// <summary>Враховуються лише завершені матчі з визначеним переможцем.</summary>
        private static IEnumerable<Match> Decided(IEnumerable<Match> matches) =>
            matches.Where(m => m.Status == MatchStatus.Completed && m.WinnerTeamId != null);

        private static bool Involves(Match match, int teamId) =>
            match.HomeTeamId == teamId || match.AwayTeamId == teamId;

        public static TeamRecord CalculateRecord(IEnumerable<Match> matches, int teamId)
        {
            var decided = Decided(matches).Where(m => Involves(m, teamId)).ToList();

            var wins = decided.Count(m => m.WinnerTeamId == teamId);
            var played = decided.Count;
            var winRate = played == 0
                ? 0m
                : Math.Round((decimal)wins / played * 100, 1);

            return new TeamRecord(played, wins, played - wins, winRate);
        }

        /// <summary>
        /// Серія рахується від найновішого завершеного матчу назад. Заплановані та
        /// скасовані матчі просто ігноруються — вони не перериваюсь серію.
        /// </summary>
        public static Streak? CalculateStreak(IEnumerable<Match> matches, int teamId)
        {
            var ordered = Decided(matches)
                .Where(m => Involves(m, teamId))
                .OrderByDescending(m => m.ScheduledAt)
                .ToList();

            if (ordered.Count == 0)
            {
                return null;
            }

            var latestIsWin = ordered[0].WinnerTeamId == teamId;
            var count = 0;

            foreach (var match in ordered)
            {
                if ((match.WinnerTeamId == teamId) != latestIsWin)
                {
                    break;
                }

                count++;
            }

            return new Streak(latestIsWin ? ResultType.Win : ResultType.Loss, count);
        }
    }
}
