namespace TForge.Common
{
    /// <summary>
    /// Що робить крок драфту.
    ///
    /// Decider стоїть окремо від Pick навмисно: його ніхто не обирає — коли в
    /// пулі лишається одна карта, сервер проставляє її сам. Тому в цього кроку
    /// немає команди-виконавця, і саме тому його не можна плутати з піком.
    /// </summary>
    public static class DraftActions
    {
        public const string Ban = "Ban";
        public const string Pick = "Pick";

        /// <summary>Сторона: CT/T, Attack/Defense, Radiant/Dire, Blue/Red.</summary>
        public const string Side = "Side";

        /// <summary>Карта, що лишилась. Виконавця немає.</summary>
        public const string Decider = "Decider";

        public static readonly string[] All = { Ban, Pick, Side, Decider };

        public static bool IsValid(string? action) => action != null && All.Contains(action);

        /// <summary>
        /// Чи вибір цього кроку вилучає значення з пулу. Сторону обирають на
        /// кожній карті окремо, тож «CT» може повторитися; забанену карту —
        /// ні.
        /// </summary>
        public static bool ConsumesFromPool(string action) => action != Side;
    }
}
