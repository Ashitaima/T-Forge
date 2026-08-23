using System.Globalization;

namespace TForge.Common
{
    /// <summary>
    /// Зсуви від UTC — у хвилинах, а не в годинах.
    ///
    /// Хвилини, бо Індія — це UTC+5:30, Непал — UTC+5:45, а Чатем — UTC+12:45:
    /// цілим числом годин їх не записати. Число, а не підпис, бо порівнює це
    /// поле сам SQL: стрічка LFT відбирає гравців у вікні зсувів, а розбирати
    /// «UTC+05:45» усередині запиту неможливо.
    ///
    /// Країна цього не замінює: Росія охоплює одинадцять зон, США — шість.
    ///
    /// Дзеркало на клієнті — Esport-Frontend/src/constants/timeZones.ts.
    /// </summary>
    public static class TimeZones
    {
        /// <summary>Зсуви, що справді використовуються, за зростанням.</summary>
        public static readonly int[] AllMinutes =
        {
            -720, -660, -600, -570, -540, -480, -420, -360, -300, -240, -210,
            -180, -120, -60,
            0,
            60, 120, 180, 210, 240, 270, 300, 330, 345, 360, 390, 420, 480,
            540, 570, 600, 630, 660, 720, 765, 780, 840
        };

        /// <summary>Null означає «гравець не вказав» і валідний скрізь.</summary>
        public static bool IsValid(int? minutes) =>
            minutes == null || AllMinutes.Contains(minutes.Value);

        /// <summary>
        /// «UTC+05:45». Підпис для журналів і тестів; на екрані малює клієнт.
        /// </summary>
        public static string Format(int minutes)
        {
            var sign = minutes < 0 ? '-' : '+';
            var absolute = Math.Abs(minutes);

            return string.Format(
                CultureInfo.InvariantCulture,
                "UTC{0}{1:00}:{2:00}", sign, absolute / 60, absolute % 60);
        }

        /// <summary>
        /// Чи потрапляє зсув у вікно; межи включні, null з будь-якого боку
        /// знімає межу.
        ///
        /// Гравець без зсуву проходить лише у вікно без меж — так само, як
        /// гравець без рангу проходить лише у вакансію без вимоги до рангу.
        /// Інакше незаповнене поле мовчки відкривало б вакансії, вимог яких
        /// ніхто не перевіряв.
        /// </summary>
        public static bool IsWithin(int? offsetMinutes, int? minMinutes, int? maxMinutes)
        {
            if (offsetMinutes == null)
            {
                return minMinutes == null && maxMinutes == null;
            }

            return (minMinutes == null || offsetMinutes.Value >= minMinutes.Value)
                && (maxMinutes == null || offsetMinutes.Value <= maxMinutes.Value);
        }
    }
}
