namespace TForge.Common
{
    /// <summary>
    /// Дозволені ігрові позиції. Значення повинні збігатися з тими,
    /// що показує фронтенд у випадному списку — інакше форма надішле
    /// значення, яке валідатор відхилить.
    /// </summary>
    public static class PlayerPositions
    {
        public const string Support = "Support";
        public const string ADC = "ADC";
        public const string Mid = "Mid";
        public const string Jungle = "Jungle";
        public const string Top = "Top";
        public const string IGL = "IGL";
        public const string Entry = "Entry";
        public const string Lurker = "Lurker";
        public const string AWPer = "AWPer";
        public const string Rifler = "Rifler";

        public static readonly string[] All =
        {
            Support, ADC, Mid, Jungle, Top, IGL, Entry, Lurker, AWPer, Rifler
        };

        public static bool IsValid(string? position) => position != null && All.Contains(position);
    }
}
