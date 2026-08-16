namespace TForge.Common
{
    /// <summary>
    /// Правила для файлів аватарів. Чисті функції — тестуються без диска й HTTP.
    ///
    /// Тип визначаємо за сигнатурою на початку файлу, а не за Content-Type чи
    /// розширенням: і те, і те задає клієнт, тож «avatar.png» із текстом
    /// усередині пройшло б будь-яку перевірку за назвою.
    /// </summary>
    public static class AvatarRules
    {
        public const int MaxBytes = 2 * 1024 * 1024;

        /// <summary>Скільки байтів достатньо прочитати, щоб розпізнати формат.</summary>
        public const int HeaderBytes = 12;

        /// <summary>Розширення для розпізнаного формату або null, якщо формат не підтримується.</summary>
        public static string? DetectExtension(byte[] head)
        {
            if (head.Length >= 3 && head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF)
            {
                return ".jpg";
            }

            if (head.Length >= 8
                && head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47
                && head[4] == 0x0D && head[5] == 0x0A && head[6] == 0x1A && head[7] == 0x0A)
            {
                return ".png";
            }

            // WebP — це контейнер RIFF: "RIFF" ....(розмір).... "WEBP".
            // Перевіряємо обидві мітки, інакше сюди пройшов би, скажімо, WAV.
            if (head.Length >= 12
                && head[0] == (byte)'R' && head[1] == (byte)'I' && head[2] == (byte)'F' && head[3] == (byte)'F'
                && head[8] == (byte)'W' && head[9] == (byte)'E' && head[10] == (byte)'B' && head[11] == (byte)'P')
            {
                return ".webp";
            }

            return null;
        }
    }
}
