/**
 * Дзеркало Common/GameMaps.cs — там значення, тут підписи.
 *
 * Назви карт не перекладають: у CS2 і Valorant їх вимовляють як є, і
 * «Пил-2» ніхто не зрозуміє. Тому підпис відрізняється від значення лише
 * там, де офіційна назва пишеться інакше (Dust2 → Dust II).
 */
export const MAP_NAMES: Record<string, Record<string, string>> = {
  CS2: {
    Ancient: "Ancient",
    Anubis: "Anubis",
    Dust2: "Dust II",
    Inferno: "Inferno",
    Mirage: "Mirage",
    Nuke: "Nuke",
    Overpass: "Overpass"
  },
  Valorant: {
    Abyss: "Abyss",
    Ascent: "Ascent",
    Haven: "Haven",
    Lotus: "Lotus",
    Split: "Split",
    Summit: "Summit",
    Sunset: "Sunset"
  }
};

export const mapLabel = (game: string, value: string) => MAP_NAMES[game]?.[value] ?? value;

/** Дисципліна драфтить карти (а не героїв). */
export const gameHasMaps = (game: string) => game in MAP_NAMES;
