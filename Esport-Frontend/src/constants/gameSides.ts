/**
 * Дзеркало Common/GameSides.cs.
 *
 * CT і T лишаються як є — так їх звуть і українською. Атака й захист у
 * Valorant, навпаки, перекладають, а Radiant, Dire, Blue і Red — це назви
 * сторін карти, тож теж лишаються.
 */
export const SIDE_NAMES: Record<string, Record<string, string>> = {
  CS2: { CT: "CT", T: "T" },
  Valorant: { Attack: "Атака", Defense: "Захист" },
  Dota2: { Radiant: "Radiant", Dire: "Dire" },
  LeagueOfLegends: { Blue: "Синя сторона", Red: "Червона сторона" }
};

export const sideLabel = (game: string, value: string) => SIDE_NAMES[game]?.[value] ?? value;
