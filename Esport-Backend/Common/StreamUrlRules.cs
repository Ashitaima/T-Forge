namespace TForge.Common
{
    /// <summary>
    /// Дозволені посилання на трансляцію.
    ///
    /// Хост порівнюємо повним збігом, а не через Contains: рядок
    /// "twitch.tv.evil.com" містить "twitch.tv", тож перевірка підрядком
    /// пропустила б чужий домен.
    /// </summary>
    public static class StreamUrlRules
    {
        public static readonly string[] AllowedHosts =
        {
            "twitch.tv",
            "www.twitch.tv",
            "youtube.com",
            "www.youtube.com",
            "youtu.be"
        };

        public static bool IsValid(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return false;
            }

            // Лише https: посилання показується користувачам як зовнішнє.
            if (uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            return AllowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase);
        }
    }
}
