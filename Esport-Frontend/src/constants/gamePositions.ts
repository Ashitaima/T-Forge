import type { Game } from "./games";

/**
 * Дзеркало Esport-Backend/Common/GamePositions.cs.
 *
 * Позиції розібрані за дисциплінами: «Support» у Dota 2 і «Support» у League
 * of Legends — різні ролі, а «AWPer» у Valorant не існує взагалі. Сервер
 * відхиляє позицію, що не належить обраній грі, тож форма мусить пропонувати
 * саме той список. Змінюючи його тут, змініть і на бекенді.
 *
 * Ключі — те, що зберігає база; підписи — те, що бачить користувач.
 */
export const GAME_POSITIONS: Record<Game, readonly string[]> = {
  Valorant: ["Duelist", "Initiator", "Sentinel", "Controller"],
  CS2: ["Rifler", "Entry", "Lurker", "AWPer", "IGL"],
  Dota2: ["Carry", "Midlaner", "Offlaner", "SoftSupport", "HardSupport"],
  LeagueOfLegends: ["Top", "Jungle", "Mid", "ADC", "Support"]
};

/**
 * Підписи для позицій. Dota 2 нумерує ролі, і саме так їх називають гравці,
 * тож номер стоїть поруч із назвою.
 */
export const POSITION_LABELS: Record<string, string> = {
  Duelist: "Duelist",
  Initiator: "Initiator",
  Sentinel: "Sentinel",
  Controller: "Controller",
  Rifler: "Rifler",
  Entry: "Entry",
  Lurker: "Lurker",
  AWPer: "AWPer",
  IGL: "IGL",
  Carry: "Carry (1)",
  Midlaner: "Midlaner (2)",
  Offlaner: "Offlaner (3)",
  SoftSupport: "Soft Support (4)",
  HardSupport: "Hard Support (5)",
  Top: "Top",
  Jungle: "Jungle",
  Mid: "Mid",
  ADC: "ADC (Bot)",
  Support: "Support"
};

export const positionsFor = (game: string): readonly string[] =>
  (GAME_POSITIONS as Record<string, readonly string[]>)[game] ?? [];

/** Підпис для значення з бекенда, яке може бути й невідомим (старі дані). */
export const positionLabel = (position: string): string =>
  POSITION_LABELS[position] ?? position;
