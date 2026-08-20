using TForge.Models;

namespace TForge.Common
{
    /// <summary>
    /// Рахунок гравця в дуелях, виведений із рядків `duels`.
    ///
    /// Це навмисно окремий показник, а не додаток до
    /// Player.TotalMatches/Wins/Losses: у тих лічильників рівно одне джерело —
    /// рядки MatchPlayer, і саме воно тримає профіль, список гравців і журнал
    /// матчів узгодженими. Домішати сюди дуелі означало б завести друге
    /// джерело тієї самої цифри.
    ///
    /// Форма та сама, що в PlayerRecordCalculator, — без EF і без сервісів,
    /// тож перевіряється тестами.
    /// </summary>
    public static class DuelRecordCalculator
    {
        public record DuelRecord(int Played, int Wins, int Losses, int Draws, decimal WinRate);

        public static DuelRecord Calculate(IEnumerable<Duel> duels, int playerId)
        {
            // Рахуються лише зіграні: виклик, який ще чекає відповіді або який
            // відхилили, — це не результат.
            var counted = duels
                .Where(duel => duel.Status == DuelStatuses.Completed)
                .Where(duel => duel.ChallengerPlayerId == playerId
                               || duel.OpponentPlayerId == playerId)
                .ToList();

            var played = counted.Count;
            var draws = counted.Count(duel => duel.WinnerPlayerId == null);
            var wins = counted.Count(duel => duel.WinnerPlayerId == playerId);
            var losses = played - wins - draws;

            // Нічиї лишаються в знаменнику: 1 з 2 при одній нічиї — це 50%,
            // а не 100%. Один знак після коми, як у PlayerRecordCalculator.
            var winRate = played == 0
                ? 0m
                : Math.Round((decimal)wins / played * 100, 1);

            return new DuelRecord(played, wins, losses, draws, winRate);
        }
    }
}
