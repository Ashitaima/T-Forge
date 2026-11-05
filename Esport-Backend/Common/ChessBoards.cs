namespace TForge.Common
{
    /// <summary>Гравець, якого можна поставити на дошку. Rating — уже ефективний (ChessRatings.Effective).</summary>
    public record BoardCandidate(int PlayerId, string Nickname, string Position, int Rating);

    public record BoardPairing(int Board, BoardCandidate Home, BoardCandidate Away, bool HomeIsWhite);

    /// <summary>
    /// Розстановка командного шахового матчу по дошках. Номер дошки — це
    /// позиція з GamePositions.Chess; гравці без неї стають за рейтингом,
    /// запасні — останніми. Господарі грають білими на непарних дошках —
    /// звичайна олімпіадна домовленість.
    /// </summary>
    public static class ChessBoards
    {
        public const int BoardCount = 4;

        private static int OrderKey(string position) => position switch
        {
            "Board1" => 1,
            "Board2" => 2,
            "Board3" => 3,
            "Board4" => 4,
            "Reserve" => 6,
            _ => 5
        };

        private static List<BoardCandidate> Order(IEnumerable<BoardCandidate> team) =>
            team.OrderBy(p => OrderKey(p.Position))
                .ThenByDescending(p => p.Rating)
                .ThenBy(p => p.PlayerId)
                .Take(BoardCount)
                .ToList();

        public static IReadOnlyList<BoardPairing> Pair(IEnumerable<BoardCandidate> home, IEnumerable<BoardCandidate> away)
        {
            var h = Order(home);
            var a = Order(away);
            var count = Math.Min(h.Count, a.Count);

            return Enumerable.Range(0, count)
                .Select(i => new BoardPairing(i + 1, h[i], a[i], HomeIsWhite: i % 2 == 0))
                .ToList();
        }
    }
}
