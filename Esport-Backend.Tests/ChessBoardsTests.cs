using TForge.Common;

namespace TForge.Tests;

public class ChessBoardsTests
{
    private static BoardCandidate P(int id, string position, int rating) => new(id, $"p{id}", position, rating);

    [Fact]
    public void Pair_UsesBoardNumbers_NotListOrder()
    {
        var home = new[] { P(1, "Board2", 1500), P(2, "Board1", 1400) };
        var away = new[] { P(3, "Board1", 1600), P(4, "Board2", 1700) };

        var boards = ChessBoards.Pair(home, away);

        Assert.Equal((2, 3), (boards[0].Home.PlayerId, boards[0].Away.PlayerId));
        Assert.Equal((1, 4), (boards[1].Home.PlayerId, boards[1].Away.PlayerId));
    }

    [Fact]
    public void Pair_HomeIsWhiteOnOddBoards()
    {
        var team = Enumerable.Range(1, 4).Select(i => P(i, $"Board{i}", 1500)).ToArray();
        var other = Enumerable.Range(5, 4).Select(i => P(i, $"Board{i - 4}", 1500)).ToArray();

        Assert.Equal(new[] { true, false, true, false },
            ChessBoards.Pair(team, other).Select(b => b.HomeIsWhite).ToArray());
    }

    [Fact]
    public void Pair_WithoutPositions_OrdersByRatingAndPutsReserveLast()
    {
        var home = new[] { P(1, "Reserve", 2500), P(2, "", 1500), P(3, "", 1900) };
        var away = new[] { P(4, "", 1000), P(5, "", 1100), P(6, "", 1200) };

        var boards = ChessBoards.Pair(home, away);

        Assert.Equal(new[] { 3, 2, 1 }, boards.Select(b => b.Home.PlayerId).ToArray());
        Assert.Equal(new[] { 6, 5, 4 }, boards.Select(b => b.Away.PlayerId).ToArray());
    }

    [Fact]
    public void Pair_StopsAtTheShorterRosterAndAtFourBoards()
    {
        var six = Enumerable.Range(1, 6).Select(i => P(i, "", 1500 + i)).ToArray();
        var two = new[] { P(10, "", 1500), P(11, "", 1400) };

        Assert.Equal(2, ChessBoards.Pair(six, two).Count);
        Assert.Equal(ChessBoards.BoardCount,
            ChessBoards.Pair(six, six.Select(p => p with { PlayerId = p.PlayerId + 100 })).Count);
    }

    [Fact]
    public void OrderKeys_CoverEveryChessPosition() =>
        // ChessBoards упізнає позиції за рядками — якщо каталог зміниться, тест
        // нагадає оновити й розстановку.
        Assert.Equal(new[] { "Board1", "Board2", "Board3", "Board4", "Reserve" }, GamePositions.Chess);
}
