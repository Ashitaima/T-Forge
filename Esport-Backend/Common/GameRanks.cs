namespace TForge.Common
{
    /// <summary>
    /// Ранги за дисциплінами — чиста функція, як і решта правил у Common.
    ///
    /// Порядок у масиві — це і є порядок рангів. Ніде не розбираємо самі
    /// рядки: «Ascendant2» більший за «Ascendant1» лише тому, що стоїть
    /// далі, а не тому, що в назві є цифра.
    ///
    /// CS2 вибивається: Premier — це число, а не сходинка з назвою. Тому
    /// зберігаємо нижню межу діапазону, а підпис («20 000–24 999») малює
    /// клієнт. Так усі чотири дисципліни лишаються порівнюваними одним
    /// індексом, і окрема числова колонка не потрібна.
    ///
    /// Дзеркало на клієнті — Esport-Frontend/src/constants/gameRanks.ts,
    /// той самий поділ, що в Games і GamePositions: тут значення, там підписи.
    /// </summary>
    public static class GameRanks
    {
        public static readonly string[] Valorant =
        {
            "Iron1", "Iron2", "Iron3",
            "Bronze1", "Bronze2", "Bronze3",
            "Silver1", "Silver2", "Silver3",
            "Gold1", "Gold2", "Gold3",
            "Platinum1", "Platinum2", "Platinum3",
            "Diamond1", "Diamond2", "Diamond3",
            "Ascendant1", "Ascendant2", "Ascendant3",
            "Immortal1", "Immortal2", "Immortal3",
            "Radiant"
        };

        // Dota 2 дає кожному рангу п'ять зірок; Immortal зірок не має.
        public static readonly string[] Dota2 =
        {
            "Herald1", "Herald2", "Herald3", "Herald4", "Herald5",
            "Guardian1", "Guardian2", "Guardian3", "Guardian4", "Guardian5",
            "Crusader1", "Crusader2", "Crusader3", "Crusader4", "Crusader5",
            "Archon1", "Archon2", "Archon3", "Archon4", "Archon5",
            "Legend1", "Legend2", "Legend3", "Legend4", "Legend5",
            "Ancient1", "Ancient2", "Ancient3", "Ancient4", "Ancient5",
            "Divine1", "Divine2", "Divine3", "Divine4", "Divine5",
            "Immortal"
        };

        // League of Legends нумерує дивізіони згори вниз: IV — найнижчий,
        // I — найвищий. Тому Iron4 стоїть перед Iron1.
        public static readonly string[] LeagueOfLegends =
        {
            "Iron4", "Iron3", "Iron2", "Iron1",
            "Bronze4", "Bronze3", "Bronze2", "Bronze1",
            "Silver4", "Silver3", "Silver2", "Silver1",
            "Gold4", "Gold3", "Gold2", "Gold1",
            "Platinum4", "Platinum3", "Platinum2", "Platinum1",
            "Emerald4", "Emerald3", "Emerald2", "Emerald1",
            "Diamond4", "Diamond3", "Diamond2", "Diamond1",
            "Master", "Grandmaster", "Challenger"
        };

        /// <summary>Смуги Premier за нижньою межею рейтингу.</summary>
        public static readonly string[] CS2 =
        {
            "Premier0", "Premier5000", "Premier10000", "Premier15000",
            "Premier20000", "Premier25000", "Premier30000"
        };

        /// <summary>Ранги дисципліни; порожній масив, якщо дисципліна невідома.</summary>
        public static string[] For(string? game) => game switch
        {
            Games.Valorant => Valorant,
            Games.CS2 => CS2,
            Games.Dota2 => Dota2,
            Games.LeagueOfLegends => LeagueOfLegends,
            _ => Array.Empty<string>()
        };

        /// <summary>
        /// Чи належить ранг саме цій дисципліні. Порожній дозволений:
        /// гравець може вказати гру, не уточнюючи рангу.
        /// </summary>
        public static bool IsValidFor(string? game, string? rank) =>
            string.IsNullOrEmpty(rank) || For(game).Contains(rank);

        /// <summary>
        /// Ранги, не нижчі за вказаний, — саме перелік значень, а не індекс.
        ///
        /// Порівняння «не нижче за Ascendant 2» — це порядок у масиві, а
        /// покликати IndexOf усередині запиту SQL не вміє. Denormalised-колонка
        /// з індексом розійшлася б із каталогом при першій зміні списку, тож
        /// натомість перекладаємо межу в перелік прийнятних значень, і запит
        /// стає IN (...). Найдовший перелік — 36 значень у Dota 2.
        ///
        /// Порожня межа означає «будь-який ранг» і додає порожній рядок:
        /// вакансія без вимоги мусить бачити й тих, хто рангу не вказав.
        /// Невідомий ранг не означає «будь-який» — він не означає нічого,
        /// і перелік порожній.
        /// </summary>
        public static string[] AtOrAbove(string? game, string? minRank)
        {
            var ranks = For(game);

            if (string.IsNullOrEmpty(minRank))
            {
                return ranks.Append(string.Empty).ToArray();
            }

            var floor = Array.IndexOf(ranks, minRank);

            return floor < 0 ? Array.Empty<string>() : ranks[floor..];
        }
    }
}
