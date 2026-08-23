import type { Game } from "./games";

/**
 * Дзеркало Esport-Backend/Common/GameRanks.cs.
 *
 * Порядок у масиві — це порядок рангів, і він мусить збігатися з бекендом:
 * саме за ним вакансія рахує «не нижче за». Ключі — те, що зберігає база;
 * підписи — те, що бачить користувач.
 */
export const GAME_RANKS: Record<Game, readonly string[]> = {
  Valorant: [
    "Iron1", "Iron2", "Iron3",
    "Bronze1", "Bronze2", "Bronze3",
    "Silver1", "Silver2", "Silver3",
    "Gold1", "Gold2", "Gold3",
    "Platinum1", "Platinum2", "Platinum3",
    "Diamond1", "Diamond2", "Diamond3",
    "Ascendant1", "Ascendant2", "Ascendant3",
    "Immortal1", "Immortal2", "Immortal3",
    "Radiant"
  ],
  Dota2: [
    "Herald1", "Herald2", "Herald3", "Herald4", "Herald5",
    "Guardian1", "Guardian2", "Guardian3", "Guardian4", "Guardian5",
    "Crusader1", "Crusader2", "Crusader3", "Crusader4", "Crusader5",
    "Archon1", "Archon2", "Archon3", "Archon4", "Archon5",
    "Legend1", "Legend2", "Legend3", "Legend4", "Legend5",
    "Ancient1", "Ancient2", "Ancient3", "Ancient4", "Ancient5",
    "Divine1", "Divine2", "Divine3", "Divine4", "Divine5",
    "Immortal"
  ],
  LeagueOfLegends: [
    "Iron4", "Iron3", "Iron2", "Iron1",
    "Bronze4", "Bronze3", "Bronze2", "Bronze1",
    "Silver4", "Silver3", "Silver2", "Silver1",
    "Gold4", "Gold3", "Gold2", "Gold1",
    "Platinum4", "Platinum3", "Platinum2", "Platinum1",
    "Emerald4", "Emerald3", "Emerald2", "Emerald1",
    "Diamond4", "Diamond3", "Diamond2", "Diamond1",
    "Master", "Grandmaster", "Challenger"
  ],
  CS2: [
    "Premier0", "Premier5000", "Premier10000", "Premier15000",
    "Premier20000", "Premier25000", "Premier30000"
  ]
};

/**
 * Premier — це число, а не сходинка з назвою, тож підпис показує діапазон.
 * Верхня смуга відкрита: 30 000 і вище.
 */
const CS2_LABELS: Record<string, string> = {
  Premier0: "0–4 999",
  Premier5000: "5 000–9 999",
  Premier10000: "10 000–14 999",
  Premier15000: "15 000–19 999",
  Premier20000: "20 000–24 999",
  Premier25000: "25 000–29 999",
  Premier30000: "30 000+"
};

/** «Ascendant2» → «Ascendant 2». Ранги не перекладають — їх так і вимовляють. */
const spaceBeforeDigits = (value: string) => value.replace(/(\d+)$/, " $1");

export const ranksFor = (game: string): readonly string[] =>
  GAME_RANKS[game as Game] ?? [];

export const rankLabel = (game: string, value: string): string => {
  if (!value) {
    return "Не вказано";
  }
  return game === "CS2" ? CS2_LABELS[value] ?? value : spaceBeforeDigits(value);
};
