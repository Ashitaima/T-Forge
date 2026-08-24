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

/**
 * Картинки карт лежать у репозиторії, а не на CDN.
 *
 * У героїв є Data Dragon і CDN Valve, у карт — нічого рівноцінного:
 * лишалися б фанатські вікі, які переїжджають і закривають хотлінк. Файли
 * названі рівно значеннями з Common/GameMaps.cs — Dust2.jpg, не dust-ii.jpg.
 *
 * eager: Vite підставляє готові URL і додає хеш. Якщо файла немає —
 * undefined, а не помилка збірки: плитка лишиться з підписом, як і при
 * відмові CDN у героїв.
 */
const MAP_IMAGES = import.meta.glob("../assets/maps/*/*.jpg", {
  eager: true,
  query: "?url",
  import: "default"
}) as Record<string, string>;

export const mapImageUrl = (game: string, value: string): string | null =>
  MAP_IMAGES[`../assets/maps/${game}/${value}.jpg`] ?? null;
