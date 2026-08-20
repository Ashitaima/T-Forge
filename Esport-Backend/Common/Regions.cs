namespace TForge.Common
{
    /// <summary>
    /// Канонічні кіберспортивні регіони. Той самий поділ, що у Games та
    /// Countries: тут значення, а підписи — на клієнті
    /// (Esport-Frontend/src/constants/regions.ts).
    ///
    /// Вільний текст давав «Europe», «EU» і «Європа» як три різні регіони, за
    /// якими не згрупувати нічого. Значення збігаються з тими, що вже лежать у
    /// базі, тож наявні команди лишаються валідними без міграції.
    /// </summary>
    public static class Regions
    {
        public const string Europe = "Europe";
        public const string NorthAmerica = "North America";
        public const string SouthAmerica = "South America";
        public const string Cis = "CIS";
        public const string Asia = "Asia";
        public const string Oceania = "Oceania";
        public const string MiddleEast = "Middle East";
        public const string Africa = "Africa";

        public static readonly string[] All =
        {
            Europe, NorthAmerica, SouthAmerica, Cis, Asia, Oceania, MiddleEast, Africa
        };

        public static bool IsValid(string? region) => region != null && All.Contains(region);
    }
}
