namespace TForge.Common
{
    /// <summary>
    /// Посилання на сторінку матчу в зовнішньому трекері статистики —
    /// tracker.gg, HLTV, Dotabuff, OP.GG, FACEIT тощо.
    ///
    /// На відміну від трансляції, список хостів тут навмисно не обмежений:
    /// у кожної дисципліни свої трекери, і закритий перелік довелося б
    /// правити щоразу. Натомість вимагаємо https і обмежуємо довжину —
    /// посилання все одно відкривається з rel="noopener noreferrer".
    /// </summary>
    public static class TrackerUrlRules
    {
        public const int MaxLength = 300;

        public static bool IsValid(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            if (url.Length > MaxLength)
            {
                return false;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return false;
            }

            // Лише https і лише справжній хост: "https:///x" чи javascript:
            // не повинні пройти.
            return uri.Scheme == Uri.UriSchemeHttps && !string.IsNullOrEmpty(uri.Host);
        }
    }
}
