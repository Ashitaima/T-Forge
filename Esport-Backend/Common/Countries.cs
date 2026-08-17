namespace TForge.Common
{
    /// <summary>
    /// Країни гравців — коди ISO 3166-1 alpha-2. У базі зберігається саме код,
    /// а прапор і назву виводить із нього фронтенд
    /// (Esport-Frontend/src/constants/countries.ts). Тому тут, як і в Games,
    /// лежать ключі, а не підписи: підписи — це UI-копія, і їхнє місце поруч
    /// із рештою українського тексту.
    /// </summary>
    public static class Countries
    {
        public static readonly string[] All =
        {
            // Європа
            "UA", "PL", "DE", "FR", "GB", "SE", "DK", "NO", "FI", "NL",
            "BE", "ES", "PT", "IT", "CH", "AT", "CZ", "SK", "HU", "RO",
            "BG", "GR", "TR", "RS", "HR", "SI", "BA", "MK", "AL", "MD",
            "LT", "LV", "EE", "IE", "IS", "RU", "BY",
            // Кавказ і Центральна Азія
            "GE", "AM", "AZ", "KZ", "UZ",
            // Америка
            "US", "CA", "MX", "BR", "AR", "CL", "PE", "CO", "UY",
            // Азія та Океанія
            "CN", "KR", "JP", "TW", "HK", "SG", "MY", "TH", "VN", "PH",
            "ID", "IN", "AU", "NZ",
            // Близький Схід і Африка
            "IL", "SA", "AE", "JO", "EG", "MA", "TN", "ZA"
        };

        public static bool IsValid(string? code) => code != null && All.Contains(code);

        /// <summary>
        /// Назви, якими країни зберігалися до переходу на коди. Потрібні один раз —
        /// щоб уже наявні профілі отримали прапор, а не лишилися з рядком, який
        /// валідатор більше не приймає.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> LegacyNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ukraine"] = "UA",
                ["Україна"] = "UA",
                ["Poland"] = "PL",
                ["Germany"] = "DE",
                ["France"] = "FR",
                ["United Kingdom"] = "GB",
                ["Great Britain"] = "GB",
                ["England"] = "GB",
                ["Sweden"] = "SE",
                ["Denmark"] = "DK",
                ["Norway"] = "NO",
                ["Finland"] = "FI",
                ["Netherlands"] = "NL",
                ["Belgium"] = "BE",
                ["Spain"] = "ES",
                ["Portugal"] = "PT",
                ["Italy"] = "IT",
                ["Switzerland"] = "CH",
                ["Austria"] = "AT",
                ["Czechia"] = "CZ",
                ["Czech Republic"] = "CZ",
                ["Slovakia"] = "SK",
                ["Hungary"] = "HU",
                ["Romania"] = "RO",
                ["Bulgaria"] = "BG",
                ["Greece"] = "GR",
                ["Turkey"] = "TR",
                ["Serbia"] = "RS",
                ["Croatia"] = "HR",
                ["Slovenia"] = "SI",
                ["Moldova"] = "MD",
                ["Lithuania"] = "LT",
                ["Latvia"] = "LV",
                ["Estonia"] = "EE",
                ["Ireland"] = "IE",
                ["Iceland"] = "IS",
                ["Russia"] = "RU",
                ["Belarus"] = "BY",
                ["Georgia"] = "GE",
                ["Armenia"] = "AM",
                ["Azerbaijan"] = "AZ",
                ["Kazakhstan"] = "KZ",
                ["Uzbekistan"] = "UZ",
                ["USA"] = "US",
                ["United States"] = "US",
                ["Canada"] = "CA",
                ["Mexico"] = "MX",
                ["Brazil"] = "BR",
                ["Argentina"] = "AR",
                ["Chile"] = "CL",
                ["Peru"] = "PE",
                ["Colombia"] = "CO",
                ["Uruguay"] = "UY",
                ["China"] = "CN",
                ["South Korea"] = "KR",
                ["Korea"] = "KR",
                ["Japan"] = "JP",
                ["Taiwan"] = "TW",
                ["Hong Kong"] = "HK",
                ["Singapore"] = "SG",
                ["Malaysia"] = "MY",
                ["Thailand"] = "TH",
                ["Vietnam"] = "VN",
                ["Philippines"] = "PH",
                ["Indonesia"] = "ID",
                ["India"] = "IN",
                ["Australia"] = "AU",
                ["New Zealand"] = "NZ",
                ["Israel"] = "IL",
                ["Saudi Arabia"] = "SA",
                ["United Arab Emirates"] = "AE",
                ["Jordan"] = "JO",
                ["Egypt"] = "EG",
                ["Morocco"] = "MA",
                ["Tunisia"] = "TN",
                ["South Africa"] = "ZA"
            };

        /// <summary>
        /// Код для значення, збереженого старою версією: сам код лишається як є,
        /// відома назва перетворюється на код, невідоме — null (нема на що міняти).
        /// </summary>
        public static string? ToCode(string? stored)
        {
            var trimmed = stored?.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                return null;
            }

            if (IsValid(trimmed))
            {
                return trimmed;
            }

            return LegacyNames.TryGetValue(trimmed, out var code) ? code : null;
        }
    }
}
