/**
 * Дзеркало Esport-Backend/Common/Games.cs.
 *
 * Значення (ключі) — це те, що зберігає база й перевіряє валідатор.
 * Підписи — те, що бачить користувач. Змінюючи список тут, змініть
 * його і на бекенді.
 */
export const GAMES = ["CS2", "Valorant", "Dota2", "LeagueOfLegends"] as const;

export type Game = (typeof GAMES)[number];

export const GAME_LABELS: Record<Game, string> = {
  CS2: "CS2",
  Valorant: "Valorant",
  Dota2: "Dota 2",
  LeagueOfLegends: "League of Legends"
};

/** Підпис для значення з бекенда, яке може бути й невідомим (старі дані). */
export const gameLabel = (game: string): string =>
  (GAME_LABELS as Record<string, string>)[game] ?? game;
