namespace TForge.Common
{
    /// <summary>
    /// Контроль часу шахової партії. Ознака моделі прогнозу: частка нічиїх
    /// помітно росте з довшим контролем. Пороги — Lichess-івські, за оцінкою
    /// тривалості «база + 40 × додавання», бо саме так розмічено датасет, на
    /// якому вчилися моделі (ml/chess/features.py — дзеркало).
    /// Дзеркало на клієнті — Esport-Frontend/src/constants/timeControls.ts.
    /// </summary>
    public static class TimeControls
    {
        public const string Bullet = "Bullet";
        public const string Blitz = "Blitz";
        public const string Rapid = "Rapid";
        public const string Classical = "Classical";

        /// <summary>Порядок — це й порядок one-hot колонок у ChessFeatures.</summary>
        public static readonly string[] All = { Bullet, Blitz, Rapid, Classical };

        /// <summary>Командні турніри без указаного контролю вважаються рапідом.</summary>
        public const string Default = Rapid;

        public static bool IsValid(string? value) => value != null && All.Contains(value);

        public static string FromClock(double baseMinutes, double incrementSeconds)
        {
            var estimatedSeconds = baseMinutes * 60 + 40 * incrementSeconds;

            if (estimatedSeconds < 180) return Bullet;
            if (estimatedSeconds < 480) return Blitz;
            if (estimatedSeconds < 1500) return Rapid;
            return Classical;
        }
    }
}
