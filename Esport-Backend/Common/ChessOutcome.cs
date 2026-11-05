namespace TForge.Common
{
    /// <summary>Імовірності з погляду кольорів — так їх видає модель.</summary>
    public readonly record struct ChessOutcome(double White, double Draw, double Black);

    /// <summary>
    /// Імовірності з погляду сторін T-Forge: перший — викликач дуелі або
    /// господар матчу. Колір — деталь, яку знає лише модель.
    /// </summary>
    public readonly record struct SideOutcome(double FirstWin, double Draw, double SecondWin)
    {
        public static SideOutcome From(ChessOutcome o, bool firstIsWhite) =>
            firstIsWhite ? new(o.White, o.Draw, o.Black) : new(o.Black, o.Draw, o.White);

        /// <summary>Коли колір невідомий — середнє двох варіантів.</summary>
        public static SideOutcome Average(SideOutcome a, SideOutcome b) =>
            new((a.FirstWin + b.FirstWin) / 2, (a.Draw + b.Draw) / 2, (a.SecondWin + b.SecondWin) / 2);

        /// <summary>Очікувані очки на дошці: перемога — 1, нічия — ½.</summary>
        public double FirstExpectedPoints => FirstWin + Draw / 2;
        public double SecondExpectedPoints => SecondWin + Draw / 2;
    }
}
