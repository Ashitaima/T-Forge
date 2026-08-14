using TForge.Models;

namespace TForge.Common
{
    /// <summary>
    /// Статистика гравця, виведена з рядків ростера. Перемога зараховується за
    /// командою, записаною в MatchPlayer.TeamId, а не за поточною командою гравця —
    /// тому трансфер не переписує минуле.
    /// </summary>
    public static class PlayerRecordCalculator
    {
        public record PlayerRecord(
            int Matches,
            int Wins,
            int Losses,
            decimal WinRate,
            int Kills,
            int Deaths,
            int Assists,
            double Kda);

        public static PlayerRecord Calculate(IEnumerable<MatchPlayer> rows)
        {
            var counted = rows
                .Where(r => r.Match != null
                            && r.Match.Status == MatchStatus.Completed
                            && r.Match.WinnerTeamId != null)
                .ToList();

            var matches = counted.Count;
            var wins = counted.Count(r => r.Match.WinnerTeamId == r.TeamId);

            var kills = counted.Sum(r => r.Kills);
            var deaths = counted.Sum(r => r.Deaths);
            var assists = counted.Sum(r => r.Assists);

            var winRate = matches == 0
                ? 0m
                : Math.Round((decimal)wins / matches * 100, 1);

            // За нуля смертей ділимо на одиницю, щоб не отримати нескінченність
            var kda = Math.Round((kills + assists) / (double)Math.Max(1, deaths), 2);

            return new PlayerRecord(matches, wins, matches - wins, winRate, kills, deaths, assists, kda);
        }
    }
}
